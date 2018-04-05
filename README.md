# OrleansCouchBaseProviders

This repository to contains a set of providers for running [Microsoft Orleans](http://github.com/dotnet/orleans) using [Couchbase](http://couchbase.com) as the storage layer.

Currently supports:

- [x] Storage Provider
- [x] Memebership Provider
- [ ] Reminders

## How to use

The storage provider can be registered like this:

``` csharp
config.Globals.RegisterStorageProvider<Orleans.Storage.OrleansCouchBaseStorage>("default", new Dictionary<string, string>
{
    { "Server", "http://localhost:8091" },
    { "UserName", "" },
    { "Password", "" },
    { "BucketName", "default" }
});
```

Password can be left blank if the bucket is not password protected. For using multiple buckets register multiple ones with different names and then use them with the `[StorageProvider(ProviderName = "provider name")]` attribute on top of grains with state.

The membership provider can be used like this:

``` csharp
config.Globals.DeploymentId = "";
config.Globals.LivenessType = GlobalConfiguration.LivenessProviderType.Custom;
config.Globals.MembershipTableAssembly = "CouchBaseProviders";

config.Globals.RegisterStorageProvider<Orleans.Storage.OrleansCouchBaseStorage>("default", new Dictionary<string, string>
{
    { "Server", "http://localhost:8091" },
    { "UserName", "" },
    { "Password", "" },
    { "BucketName", "default" }
});
config.Globals.RegisterStorageProvider<Orleans.Storage.OrleansCouchBaseStorage>("PubSubStore", new Dictionary<string, string>
{
    { "Server", "http://localhost:8091" },
    { "UserName", "" },
    { "Password", "" },
    { "BucketName", "default" }
});
config.Globals.DataConnectionString = "http://localhost:8091";
config.PrimaryNode = null;
config.Globals.SeedNodes.Clear();
```

NOTE: The membership provider requires a bucket called `membership`.

## Document expiry per grain type

By default documents written to Couchbase will not have an expiry value set.

Support has now been added to allow expiry values to be configured per grain type.

To use this feature you need to update your app.config or web.config file;

### Add config section declaration

Add the following under the <configSections> element:

``` xml
<section name="orleans" type="CouchBaseProviders.Configuration.CouchbaseOrleansDocumentExpiry.CouchbaseOrleansConfigurationSection, CouchbaseProviders" />
```

### Add the config section with per grain expiry values:

``` xml
<orleans>
	<grainExpiry>
	  <add grainType="grainX" expiresIn="0:0:1:0"></add>
	</grainExpiry>
</orleans>
```

The expiresIn value must be a valid TimeSpan format. Examples include:

10 seconds: 00:00:10
10 minutes: 00:10:00
10 hours: 10:00:00
10 days: 10:00:00:00

Refer to the app.confg provided in the CouchBaseStorageTests project for more information.

## How to help

Take a look at the current issues and report any issues you find.
The providers have been tested with CouchBase Community 4.1.

## License

The [MIT](LICENSE) license.
