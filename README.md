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

## Do you want to help?

Take a look at issues anhd also test it and report issues. We've tested this on CouchBase Community 4.1.
You can also write more Unit Tests if you will.

# License

The [MIT](LICENSE) license.
