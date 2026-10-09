using MatchingSports.DemoSimulator;
using Microsoft.Extensions.Configuration;

if (args.Contains("--help", StringComparer.Ordinal))
{
    Console.WriteLine("DemoSimulator --users 50 --teams 6 --listings 15 --run-id cv-demo-01 --mode demo|test --output <directory>");
    Console.WriteLine("Secrets: ConnectionStrings__matchingsports và Demo__Password, hoặc user-secrets.");
    return 0;
}
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) => { eventArgs.Cancel = true; cancellation.Cancel(); };
try
{
    var options = DemoOptions.Parse(args);
    var configuration = new ConfigurationBuilder().AddUserSecrets(typeof(DemoRunner).Assembly, optional: true)
        .AddEnvironmentVariables().Build();
    var report = await DemoRunner.RunAsync(configuration, options, cancellation.Token);
    Console.WriteLine($"Hoàn tất {report.RunId}: {report.Users.Count} users, {report.Teams.Count} teams, {report.Listings.Count} kèo.");
    Console.WriteLine($"Thông báo: {report.Notifications}; watch deliveries: {report.WatchDeliveries}; nhắc lịch: {report.ReminderHistories}.");
    Console.WriteLine(options.TestMode ? "Database test đã được dọn." : "Dữ liệu demo được giữ để đăng nhập qua API/Swagger.");
    Console.WriteLine($"Báo cáo: {Path.GetFullPath(options.OutputDirectory)} (theo database và RunId).");
    return 0;
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Console.Error.WriteLine("Đã dừng; chạy lại cùng RunId ở chế độ demo để tiếp tục.");
    return 130;
}
catch (DemoSimulationException exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
catch (Exception exception)
{
    // Provider exceptions can contain connection credentials or payloads.
    Console.Error.WriteLine($"Mô phỏng thất bại ({exception.GetType().Name}); xem activeStep trong checkpoint và kiểm tra cấu hình.");
    return 1;
}
