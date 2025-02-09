namespace InlineSqlSharp;

// This class is public to define the ISqlElement interface,
// but not for external use.
public sealed class SqlBuildingBuffer
{
	private readonly SqlBuildingBufferCore _core;

	internal SqlBuildingBuffer()
	{
		_core = new(this);
	}

	internal SqlBuildingBufferCore Core => _core;

	internal void Dispose()
	{
		Core.Dispose();
	}
}
