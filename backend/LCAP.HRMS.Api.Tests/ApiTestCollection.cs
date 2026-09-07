using Xunit;

namespace LCAP.HRMS.Api.Tests;

// Hosts watch the same configuration files. Serialize lifecycle/teardown on Windows.
[CollectionDefinition("API integration", DisableParallelization = true)]
public sealed class ApiTestCollection;
