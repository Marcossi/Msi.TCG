using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Msi.TemplateCodeGenerator.Messages;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.Services;

namespace Msi.TemplateCodeGenerator.Tests.Services;

public class FileWatcherServiceTests : IDisposable
{
    private readonly string _tempDir;

    public FileWatcherServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task StartWatching_WhenFileChanged_SendsMessengerMessage()
    {
        WeakReferenceMessenger messenger = new();
        FileWatcherService service = new(messenger, NullLogger<FileWatcherService>.Instance);
        TaskCompletionSource<ProjectFilesChangedMessage> tcs = new();

        messenger.Register<ProjectFilesChangedMessage>(this, (_, m) => tcs.TrySetResult(m));

        service.StartWatching(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "test.scriban"), "content");

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
        ProjectFilesChangedMessage message = await tcs.Task.WaitAsync(cts.Token);

        Assert.NotNull(message.RelativePath);
        Assert.Equal(FileChangeType.Created, message.ChangeType);

        service.Dispose();
    }

    [Fact]
    public async Task StopWatching_DoesNotSendMessages()
    {
        WeakReferenceMessenger messenger = new();
        FileWatcherService service = new(messenger, NullLogger<FileWatcherService>.Instance);

        service.StartWatching(_tempDir);
        service.StopWatching();

        int messageCount = 0;
        messenger.Register<ProjectFilesChangedMessage>(this, (_, _) => messageCount++);

        File.WriteAllText(Path.Combine(_tempDir, "test.scriban"), "content");
        await Task.Delay(500);

        Assert.Equal(0, messageCount);

        service.Dispose();
    }

    [Fact]
    public async Task Dispose_StopsWatching()
    {
        WeakReferenceMessenger messenger = new();
        FileWatcherService service = new(messenger, NullLogger<FileWatcherService>.Instance);

        service.StartWatching(_tempDir);
        service.Dispose();

        int messageCount = 0;
        messenger.Register<ProjectFilesChangedMessage>(this, (_, _) => messageCount++);

        File.WriteAllText(Path.Combine(_tempDir, "test2.scriban"), "content");
        await Task.Delay(500);

        Assert.Equal(0, messageCount);
    }
}
