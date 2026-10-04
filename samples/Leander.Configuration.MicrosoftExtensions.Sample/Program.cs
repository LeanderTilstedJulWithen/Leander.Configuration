using Leander.Configuration.MicrosoftExtensions.Sample;

// Each example uses the same contract from ServerContract.cs.
ReadingIConfiguration.Run();

ScopedSnapshots.Run();

return await HostRegistration.RunAsync(args);
