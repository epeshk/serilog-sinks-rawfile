using System.Text;
using Serilog.Sinks.File;
using Serilog.Sinks.RawFile;

namespace Serilog.Sinks.RawFile;

public class RawFileLifecycleHooksAdapter : RawFileLifecycleHooks
{
  readonly FileLifecycleHooks hooks;

  public RawFileLifecycleHooksAdapter(FileLifecycleHooks hooks)
  {
    this.hooks = hooks;
  }

  public override Stream OnFileOpened(Stream underlyingStream, Encoding encoding)
  {
    return hooks.OnFileOpened(underlyingStream, encoding);
  }

  public override Stream OnFileOpened(string path, Stream underlyingStream, Encoding encoding)
  {
    return hooks.OnFileOpened(path, underlyingStream, encoding);
  }

  public override void OnFileDeleting(string path)
  {
    hooks.OnFileDeleting(path);
  }

  public override string? ToString()
  {
    return hooks.ToString();
  }
}
