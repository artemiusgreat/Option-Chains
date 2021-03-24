using Client.EnumSpace;
using Core.EnumSpace;
using System.Reactive.Subjects;

namespace Client.ServiceSpace
{
  public class Message
  {
    public dynamic Content { get; set; }
    public ActionEnum Action { get; set; }
    public EntityEnum Entity { get; set; }
  }

  public class CommandService
  {
    /// <summary>
    /// Cache
    /// </summary>
    public static ISubject<Message> Events { get; set; } = new Subject<Message>();
  }
}
