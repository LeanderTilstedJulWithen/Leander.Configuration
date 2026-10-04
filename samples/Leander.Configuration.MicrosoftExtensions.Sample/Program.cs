using Leander.Configuration.MicrosoftExtensions.Sample;

// Each example uses the same contract from ServerComtract.cs.
ReadingIConfiguration.Run();

ScopedSnapshots.Run();

return await HostRegistration.RunAsync(args);
