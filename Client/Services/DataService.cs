using Core.ModelSpace;
using Gateway.Tradier;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace Client.ServiceSpace
{
  public class DataService
  {
    private static GatewayModel _gateway = null;

    /// <summary>
    /// Create gateway instance if needed
    /// </summary>
    /// <returns></returns>
    public static async Task<GatewayModel> GetGateway()
    {
      if (_gateway == null)
      {
        var account = new AccountModel
        {
          Name = "Options",
          Id = ConfigurationManager.AppSettings["Account"]
        };

        _gateway = new GatewayClient()
        {
          Name = "Options",
          Account = account,
          LiveToken = ConfigurationManager.AppSettings["TradierLiveToken"].ToString(),
          SandboxToken = ConfigurationManager.AppSettings["TradierSandboxToken"].ToString()
        };

        await _gateway.Connect();
      }

      return _gateway;
    }

    /// <summary>
    /// Load option chain
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns></returns>
    public static async Task<IList<IInstrumentOptionModel>> GetOptionChain(IDictionary<dynamic, dynamic> inputs)
    {
      return await (await GetGateway()).GetOptionChains(inputs);
    }

    /// <summary>
    /// Load instrument data
    /// </summary>
    /// <param name="names"></param>
    /// <returns></returns>
    public static async Task<IDictionary<string, IInstrumentModel>> GetInstruments(IList<string> names)
    {
      names.Add("X");

      return (await (await GetGateway()).GetInstruments(new Dictionary<dynamic, dynamic>
      {
        ["greeks"] = true,
        ["symbols"] = string.Join(",", names)

      })).ToDictionary(o => o.Name);
    }
  }
}
