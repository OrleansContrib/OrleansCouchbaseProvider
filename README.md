# OrleansCouchBaseProviders

This repository aims to contain a set of providers for running [Microsoft Orleans](http://github.com/dotnet/orleans) using CouchBase as storage for everything. 

Currently a Storage Provider and membership provider are included but reminders are to be done yet.

## How to use

The storage provider can be registered like this:

``` csharp
			config.Globals.RegisterStorageProvider<Orleans.Storage.OrleansCouchBaseStorage>("Default",
                    new Dictionary<string, string>()
                            {
                                { "Server","http://localhost:8091" },
                                { "UserName","" },
                                { "Password","" },
                                { "BucketName","default" }
                            });
```

Password can be left blank if the bucket is not password protected. for using multiple buckets register multiple ones with different names and then use them with the `[StorageProvider(ProviderName = "provider name")]` attribute on top of grains with state.

The membership provider can be used like this

``` csharp
				config.Globals.DeploymentId = "";
                config.Globals.LivenessType = GlobalConfiguration.LivenessProviderType.Custom;
                config.Globals.MembershipTableAssembly = "CouchBaseProviders";

                config.Globals.RegisterStorageProvider<Orleans.Storage.OrleansCouchBaseStorage>("Default", new Dictionary<string, string>() {
                { "Server","http://localhost:8091" },
                { "UserName","" },
                { "Password","" },
                { "BucketName","default" }
            });
                config.Globals.RegisterStorageProvider<Orleans.Storage.OrleansCouchBaseStorage>("PubSubStore", new Dictionary<string, string>() {
                { "Server","http://localhost:8091" },
                { "UserName","" },
                { "Password","" },
                { "BucketName","default" }
            });
                config.Globals.DataConnectionString = "http://localhost:8091";
                config.PrimaryNode = null;
                config.Globals.SeedNodes.Clear();
            }
```

## Bucket configuration

For using the membership provider, you need a bucket named membership and you can give it minimum RAM and storage because it will store a little amount of data.

For your own usage, use one bucket only as couchbase advices itself and don't go over 10 buckets. You don't need much RAM for your own buckets as well since Orleans will keep the state of hot actors in memory itself.

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

- 10 seconds: 00:00:10
- 10 minutes: 00:10:00
- 10 hours: 10:00:00
- 10 days: 10:00:00:00

Refer to the app.confg provided in the CouchBaseStorageTests project for more information.

## Do you want to help?

Take a look at issues anhd also test it and report issues. We've tested this on CouchBase Community 4.1.
You can also write more Unit Tests if you will.

# License

The [MIT](LICENSE) license.
