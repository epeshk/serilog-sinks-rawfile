using Serilog.Sinks.File;

namespace Serilog.Sinks.RawFile;

public static class FileLifecycleHooksExtensions
{
  /// <summary>
  /// Wraps Serilog.Sinks.File <paramref name="hooks"/> into <see cref="RawFileLifecycleHooks"/>.
  /// </summary>
  /// <param name="hooks">A hook to wrap</param>
  /// <returns>Wrapper implementing <see cref="RawFileLifecycleHooks"/> from Serilog.Sinks.RawFile.</returns>
  public static RawFileLifecycleHooks Wrap(this FileLifecycleHooks hooks) => new RawFileLifecycleHooksAdapter(hooks);
}
