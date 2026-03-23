namespace PolyEdgeScout.Infrastructure.Tests.Logging;

using PolyEdgeScout.Infrastructure.Logging;

public sealed class FileLogServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileLogService _sut;

    public FileLogServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"FileLogServiceTests_{Guid.NewGuid():N}");
        _sut = new FileLogService(_tempDir);
    }

    public void Dispose()
    {
        _sut.Dispose();

        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Info_DoesNotWriteToConsole()
    {
        // Arrange — redirect console output
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            // Act
            _sut.Info("test message");

            // Assert — nothing should appear on stdout
            Assert.Equal(string.Empty, sw.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void LogEntryWritten_FiresWithCorrectLevelAndFormattedLine()
    {
        // Arrange
        string? receivedLevel = null;
        string? receivedLine = null;
        _sut.LogEntryWritten += (level, line) =>
        {
            receivedLevel = level;
            receivedLine = line;
        };

        // Act
        _sut.Info("hello world");

        // Assert
        Assert.Equal("INF", receivedLevel);
        Assert.NotNull(receivedLine);
        Assert.Contains("[INF]", receivedLine);
        Assert.Contains("hello world", receivedLine);
    }

    [Theory]
    [InlineData("INF")]
    [InlineData("WRN")]
    [InlineData("ERR")]
    [InlineData("DBG")]
    public void LogEntryWritten_FiresForEachLogLevel(string expectedLevel)
    {
        // Arrange
        string? receivedLevel = null;
        string? receivedLine = null;
        _sut.LogEntryWritten += (level, line) =>
        {
            receivedLevel = level;
            receivedLine = line;
        };

        // Act
        switch (expectedLevel)
        {
            case "INF": _sut.Info("msg"); break;
            case "WRN": _sut.Warn("msg"); break;
            case "ERR": _sut.Error("msg"); break;
            case "DBG": _sut.Debug("msg"); break;
        }

        // Assert
        Assert.Equal(expectedLevel, receivedLevel);
        Assert.NotNull(receivedLine);
        Assert.Contains($"[{expectedLevel}]", receivedLine);
        Assert.Contains("msg", receivedLine);
    }

    [Fact]
    public void Error_WithException_FiresEventWithFormattedDetails()
    {
        // Arrange
        string? receivedLevel = null;
        string? receivedLine = null;
        _sut.LogEntryWritten += (level, line) =>
        {
            receivedLevel = level;
            receivedLine = line;
        };

        var ex = new InvalidOperationException("boom");

        // Act
        _sut.Error("something failed", ex);

        // Assert
        Assert.Equal("ERR", receivedLevel);
        Assert.NotNull(receivedLine);
        Assert.Contains("something failed", receivedLine);
        Assert.Contains("InvalidOperationException", receivedLine);
        Assert.Contains("boom", receivedLine);
    }

    [Fact]
    public void Write_DoesNotWriteToConsole_ForAnyLevel()
    {
        // Arrange — redirect console output
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            // Act
            _sut.Info("i");
            _sut.Warn("w");
            _sut.Error("e");
            _sut.Debug("d");

            // Assert
            Assert.Equal(string.Empty, sw.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
