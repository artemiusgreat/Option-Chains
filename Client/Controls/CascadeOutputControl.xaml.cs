using Chart;
using Chart.ModelSpace;
using Chart.SeriesSpace;
using Client.ModelSpace;
using Client.ServiceSpace;
using Core.EnumSpace;
using Core.ModelSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.ControlSpace
{
  /// <summary>
  /// Control class
  /// </summary>
  public partial class CascadeOutputControl : UserControl
  {
    private string _name = "Options";
    private IList<Chart.ModelSpace.IPointModel> _points = new List<Chart.ModelSpace.IPointModel>();

    public bool Variance { get; set; } = true;
    public IList<ChainInputModel> Chains { get; set; }
    public Func<IInstrumentOptionModel, bool> Where { get; set; } = item => true;
    public Func<IInstrumentOptionModel, double?> Select { get; set; } = item => 0.0;
    public Func<IEnumerable<IInstrumentOptionModel>, double?> SelectMany { get; set; } = items => 0.0;

    /// <summary>
    /// Constructor
    /// </summary>
    public CascadeOutputControl()
    {
      InitializeComponent();
    }

    /// <summary>
    /// Render charts
    /// </summary>
    public void CreateCharts()
    {
      View.Composer = new ComponentComposer
      {
        Name = _name,
        Control = View,
        Group = new AreaModel(),
        ShowIndexAction = i =>
        {
          dynamic point = _points.ElementAtOrDefault((int)i);

          if (point == null)
          {
            return null;
          }

          return $"{ point.Strike }";
        }
      };

      View.Composer.Create();
      View.Composer.Update();

      var clock = new Timer();

      clock.Interval = 1000;
      clock.Enabled = true;
      clock.Elapsed += async (object sender, ElapsedEventArgs e) =>
      {
        clock.Stop();
        await OnData();
      };
    }

    /// <summary>
    /// Data updates
    /// </summary>
    private async Task OnData()
    {
      var instrument = (await DataService
        .GetInstruments(Chains.Select(o => o.Symbol).ToList()))
        .FirstOrDefault()
        .Value;

      if (instrument == null)
      {
        return;
      }

      // Get chains

      var optionQueries = Chains.Select(chain =>
      {
        var inputs = new Dictionary<dynamic, dynamic>
        {
          ["greeks"] = Variance,
          ["symbol"] = chain.Symbol,
          ["expiration"] = chain.Expiration
        };

        return DataService.GetOptionChain(inputs);
      });

      var options = (await Task
        .WhenAll(optionQueries))
        .SelectMany(o => o)
        .GroupBy(o => o.Strike)
        .ToDictionary(o => o.Key, o => o.Where(v => Where(v)).ToList())
        .OrderBy(o => o.Key)
        .ToList();

      // Find the current strike

      var closeIndex = -1;
      var closeDistance = double.MaxValue;

      for (var i = 0; i < options.Count; i++)
      {
        var distance = Math.Abs(instrument.Point.Price.Value - options[i].Key.Value);

        if (closeDistance > distance)
        {
          closeIndex = i;
          closeDistance = distance;
        }
      }

      if (closeIndex < 0)
      {
        return;
      }

      // Aggregate around the current strike

      var index = 0;
      var minSum = 0.0;
      var maxSum = 0.0;
      var summaries = new InstrumentOptionModel[options.Count];

      while (true)
      {
        var curMins = options.ElementAtOrDefault(closeIndex - index);
        var curMaxs = options.ElementAtOrDefault(closeIndex + index);

        if (curMins.Value == null && curMaxs.Value == null)
        {
          break;
        }

        if (curMins.Value != null)
        {
          minSum += SelectMany(curMins.Value).Value;
          summaries[closeIndex - index] = new InstrumentOptionModel
          {
            Strike = curMins.Key,
            Side = OptionSideEnum.Put,
            Point = new Core.ModelSpace.PointModel { Price = minSum }
          };
        }

        if (curMaxs.Value != null)
        {
          maxSum += SelectMany(curMaxs.Value).Value;
          summaries[closeIndex + index] = new InstrumentOptionModel
          {
            Strike = curMaxs.Key,
            Side = OptionSideEnum.Call,
            Point = new Core.ModelSpace.PointModel { Price = maxSum }
          };
        }

        index++;
      }

      // Create points with values for each expiration

      var points = summaries.Select(o =>
      {
        var strike = o.Strike;
        var series = new Dictionary<string, ISeriesModel>();
        var seriesColor = Brushes.Black.Color;
        var seriesName = "Aggregation";

        switch (o.Side)
        {
          case OptionSideEnum.Put: seriesColor = Brushes.OrangeRed.Color; break;
          case OptionSideEnum.Call: seriesColor = Brushes.LimeGreen.Color; break;
        }

        series[seriesName] = new SeriesModel
        {
          Name = seriesName,
          Shape = new BarSeries(),
          Model = new ExpandoModel
          {
            ["Color"] = seriesColor,
            ["Point"] = o.Point.Price
          }
        };

        // Create point with all series

        var point = new Chart.ModelSpace.PointModel()
        {
          Areas = new Dictionary<string, IAreaModel>
          {
            [_name] = new AreaModel
            {
              Name = _name,
              Series = series
            }
          }
        };

        point["Strike"] = strike;

        return point as Chart.ModelSpace.IPointModel;
      });

      // Set appropriate number of series on the chart 

      if (points.Any())
      {
        View.Composer.Group.Series = points.First().Areas[_name].Series;
      }

      await Dispatcher.BeginInvoke(new Action(() =>
      {
        var composer = View.Composer;

        composer.IndexDomain ??= new int[2];
        composer.IndexDomain[0] = 0;
        composer.IndexDomain[1] = options.Count;
        composer.Items = _points = points.ToList();
        composer.Update();
      }));
    }
  }
}
