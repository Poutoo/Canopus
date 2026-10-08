// Strings is a process-wide static (language loaded once at startup): tests that switch
// language must not run in parallel.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
