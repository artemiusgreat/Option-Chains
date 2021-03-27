using Core.EnumSpace;

namespace Gateway.Tradier.ModelSpace
{
  public class OptionTypeMap
  {
    public static OptionSideEnum? Input(string side)
    {
      switch ($"{ side }".ToUpper())
      {
        case "PUT": return OptionSideEnum.Put;
        case "CALL": return OptionSideEnum.Call;
      }

      return null;
    }
  }
}
