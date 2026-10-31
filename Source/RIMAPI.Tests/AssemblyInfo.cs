// The tests drive LogApi's static settings, so they must not run side by side.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
