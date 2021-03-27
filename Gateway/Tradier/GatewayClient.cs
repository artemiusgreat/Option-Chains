using Core.EnumSpace;
using Core.ManagerSpace;
using Core.ModelSpace;
using Gateway.Tradier.ModelSpace;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Websocket.Client;

namespace Gateway.Tradier
{
  /// <summary>
  /// Implementation
  /// </summary>
  public class GatewayClient : GatewayModel, IGatewayModel
  {
    /// <summary>
    /// Socket session ID
    /// </summary>
    protected string _streamSession = null;

    /// <summary>
    /// API key
    /// </summary>
    public string Token
    {
      get
      {
        switch (Mode)
        {
          case EnvironmentEnum.Live: return LiveToken;
          case EnvironmentEnum.Sandbox: return SandboxToken;
        }

        return null;
      }
    }

    /// <summary>
    /// HTTP endpoint
    /// </summary>
    public string Source
    {
      get
      {
        switch (Mode)
        {
          case EnvironmentEnum.Live: return LiveSource;
          case EnvironmentEnum.Sandbox: return SandboxSource;
        }

        return null;
      }
    }

    /// <summary>
    /// API key
    /// </summary>
    public string LiveToken { get; set; }

    /// <summary>
    /// Sandbox API key
    /// </summary>
    public string SandboxToken { get; set; }

    /// <summary>
    /// HTTP endpoint
    /// </summary>
    public string LiveSource { get; set; } = "https://api.tradier.com";

    /// <summary>
    /// Sandbox HTTP endpoint
    /// </summary>
    public string SandboxSource { get; set; } = "https://sandbox.tradier.com";

    /// <summary>
    /// Socket endpoint
    /// </summary>
    public string StreamSource { get; set; } = "wss://ws.tradier.com/v1";

    /// <summary>
    /// Establish connection with a server
    /// </summary>
    /// <param name="docHeader"></param>
    public override Task Connect()
    {
      return Task.Run(async () =>
      {
        try
        {
          await Disconnect();

          _connections.Add(_serviceClient ??= new ClientService());

          //await GetAccountData();
          //await GetActiveOrders();
          //await GetActivePositions();
          await Subscribe();
        }
        catch (Exception e)
        {
          InstanceManager<LogService>.Instance.Log.Error(e.ToString());
        }
      });
    }

    /// <summary>
    /// Disconnect
    /// </summary>
    /// <returns></returns>
    public override Task Disconnect()
    {
      Unsubscribe();

      _connections.ForEach(o => o.Dispose());
      _connections.Clear();

      return Task.FromResult(0);
    }

    /// <summary>
    /// Start streaming
    /// </summary>
    /// <returns></returns>
    public override async Task Subscribe()
    {
      await Unsubscribe();

      // Orders

      var orderSubscription = OrderSenderStream.Subscribe(message =>
      {
        switch (message.Action)
        {
          case ActionEnum.Create: CreateOrders(message.Next); break;
          case ActionEnum.Update: UpdateOrders(message.Next); break;
          case ActionEnum.Delete: DeleteOrders(message.Next); break;
        }
      });

      _subscriptions.Add(orderSubscription);

      // Streaming

      _streamSession = await GetStreamSession();

      var client = new WebsocketClient(new Uri(StreamSource + "/markets/events"), _streamOptions)
      {
        Name = Account.Name,
        ReconnectTimeout = TimeSpan.FromSeconds(30),
        ErrorReconnectTimeout = TimeSpan.FromSeconds(30)
      };

      var connectionSubscription = client.ReconnectionHappened.Subscribe(message => { });
      var disconnectionSubscription = client.DisconnectionHappened.Subscribe(message => { });
      var messageSubscription = client.MessageReceived.Subscribe(message =>
      {
        dynamic input = JObject.Parse(message.Text);

        switch ($"{ input.type }")
        {
          case "quote":

            OnInputQuote(ConversionManager.Deserialize<InputPointModel>(message.Text));
            break;

          case "trade": break;
          case "tradex": break;
          case "summary": break;
          case "timesale": break;
        }
      });

      _subscriptions.Add(messageSubscription);
      _subscriptions.Add(connectionSubscription);
      _subscriptions.Add(disconnectionSubscription);
      _subscriptions.Add(client);

      await client.Start();

      var query = new
      {
        linebreak = true,
        advancedDetails = true,
        sessionid = _streamSession,
        symbols = Account.Instruments.Values.Select(o => o.Name)
      };

      client.Send(ConversionManager.Serialize(query));
    }

    public override Task Unsubscribe()
    {
      _subscriptions.ForEach(o => o.Dispose());
      _subscriptions.Clear();

      return Task.FromResult(0);
    }

    public override Task<IList<IAccountModel>> GetAccounts(IDictionary<dynamic, dynamic> inputs)
    {
      throw new NotImplementedException();
    }

    public override async Task<IList<IInstrumentModel>> GetInstruments(IDictionary<dynamic, dynamic> inputs)
    {
      var instruments = new List<IInstrumentModel>();
      var response = await GetResponse<InputInstrumentDataModel>($"/v1/markets/quotes", inputs);

      foreach (var inputPoint in response?.Quotes?.Quote ?? new List<InputInstrumentModel>())
      {
        var pointModel = new PointModel
        {
          Ask = inputPoint.Ask,
          Bid = inputPoint.Bid,
          AskSize = inputPoint.AskSize,
          BidSize = inputPoint.BidSize,
          Price = inputPoint.Price
        };

        var instrumentModel = new InstrumentModel
        {
          Point = pointModel,
          Name = inputPoint.Symbol,
          Volume = inputPoint.Volume
        };

        instruments.Add(instrumentModel);
      }

      return instruments;
    }

    public override Task<IList<IPointModel>> GetPoints(IDictionary<dynamic, dynamic> inputs)
    {
      throw new NotImplementedException();
    }

    public override Task<IList<ITransactionOrderModel>> GetOrders(IDictionary<dynamic, dynamic> inputs)
    {
      throw new NotImplementedException();
    }

    public override Task<IList<ITransactionPositionModel>> GetPositions(IDictionary<dynamic, dynamic> inputs)
    {
      throw new NotImplementedException();
    }

    public override Task<IEnumerable<ITransactionOrderModel>> CreateOrders(params ITransactionOrderModel[] orders)
    {
      throw new NotImplementedException();
    }

    public override Task<IEnumerable<ITransactionOrderModel>> UpdateOrders(params ITransactionOrderModel[] orders)
    {
      throw new NotImplementedException();
    }

    public override Task<IEnumerable<ITransactionOrderModel>> DeleteOrders(params ITransactionOrderModel[] orders)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Get options strikes
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns></returns>
    public override async Task<IList<double>> GetOptionPrices(IDictionary<dynamic, dynamic> inputs)
    {
      var response = await GetResponse<InputOptionStrikeDataModel>($"/v1/markets/options/strikes", inputs);

      return response?.Strikes?.Strike ?? new List<double>();
    }

    /// <summary>
    /// Get options expiration dates
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns></returns>
    public override async Task<IList<DateTime>> GetOptionDates(IDictionary<dynamic, dynamic> inputs)
    {
      var response = await GetResponse<InputOptionDateDataModel>($"/v1/markets/options/expirations", inputs);

      return response?.Dates?.Date ?? new List<DateTime>();
    }

    /// <summary>
    /// Get options chain
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns></returns>
    public override async Task<IList<IInstrumentOptionModel>> GetOptionChains(IDictionary<dynamic, dynamic> inputs)
    {
      var options = new List<IInstrumentOptionModel>();
      var response = await GetResponse<InputOptionChainDataModel>($"/v1/markets/options/chains", inputs);

      foreach (var inputOption in response?.Chains?.Chain ?? new List<InputOptionModel>())
      {
        var pointModel = new PointModel
        {
          Bid = inputOption.Bid,
          Ask = inputOption.Ask,
          Price = inputOption.Last,
          BidSize = inputOption.BidSize,
          AskSize = inputOption.AskSize
        };

        var optionModel = new InstrumentOptionModel
        {
          Point = pointModel,
          Strike = inputOption.Strike,
          Volume = inputOption.Volume,
          AverageVolume = inputOption.AverageVolume,
          ContractSize = inputOption.ContractSize,
          OpenInterest = inputOption.OpenInterest,
          ExpirationDate = inputOption.ExpirationDate,
          Side = OptionTypeMap.Input(inputOption.OptionType),
          Variance = new InstrumentOptionVarianceModel
          {
            Vega = inputOption.Greeks.Vega,
            Delta = inputOption.Greeks.Delta,
            Gamma = inputOption.Greeks.Gamma,
            Theta = inputOption.Greeks.Theta,
            Interest = inputOption.Greeks.Rho,
            Distribution = inputOption.Greeks.Phi,
            AskIv = inputOption.Greeks.AskIv,
            BidIv = inputOption.Greeks.BidIv,
            Iv = inputOption.Greeks.Iv
          }
        };

        options.Add(optionModel);
      }

      return options;
    }

    /// <summary>
    /// Process incoming quotes
    /// </summary>
    /// <param name="input"></param>
    protected void OnInputQuote(InputPointModel input)
    {
      var dateAsk = input.AskDate;
      var dateBid = input.BidDate;
      var currentAsk = input.Ask;
      var currentBid = input.Bid;
      var previousAsk = _point?.Ask ?? currentAsk;
      var previousBid = _point?.Bid ?? currentBid;
      var symbol = input.Symbol;

      var point = new PointModel
      {
        Ask = currentAsk,
        Bid = currentBid,
        Bar = new PointBarModel(),
        Instrument = Account.Instruments[symbol],
        AskSize = input.AskSize,
        BidSize = input.BidSize,
        Time = DateTimeOffset.FromUnixTimeMilliseconds(Math.Max(dateAsk.Value, dateBid.Value)).DateTime,
        Last = ConversionManager.Compare(currentBid, previousBid) ? currentAsk : currentBid
      };

      _point = point;

      UpdatePointProps(point);
    }

    /// <summary>
    /// Process incoming quotes
    /// </summary>
    /// <param name="input"></param>
    protected void OnInputTrade(dynamic input)
    {
    }

    /// <summary>
    /// Create session to start streaming
    /// </summary>
    /// <returns></returns>
    protected async Task<string> GetStreamSession()
    {
      using (var sessionClient = new ClientService())
      {
        var headers = new Dictionary<dynamic, dynamic>
        {
          ["Accept"] = "application/json",
          ["Authorization"] = $"Bearer { LiveToken }"
        };

        dynamic session = await sessionClient.Post<dynamic>(LiveSource + "/v1/markets/events/session", null, headers);

        return $"{ session.stream.sessionid }";
      }
    }

    /// <summary>
    /// Send HTTP query
    /// </summary>
    /// <param name="source"></param>
    /// <param name="inputs"></param>
    /// <param name="variables"></param>
    /// <returns></returns>
    protected async Task<T> GetResponse<T>(
      string endpoint,
      IDictionary<dynamic, dynamic> inputs = null,
      IDictionary<dynamic, dynamic> variables = null)
    {
      var headers = new Dictionary<dynamic, dynamic>
      {
        ["Accept"] = "application/json",
        ["Authorization"] = $"Bearer { Token }"
      };

      return variables == null ?
        await _serviceClient.Get<T>(Source + endpoint, inputs, headers) :
        await _serviceClient.Post<T>(Source + endpoint, inputs, headers);
    }
  }
}
