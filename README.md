# OrleansCouchBaseProviders

This repository aims to contain a set of providers for running [Microsoft Orleans](http://github.com/dotnet/orleans) using CouchBase as storage for everything. 

Currently a Storage Provider is included but Membership provider and reminders are being worked on and will be pushed here.

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

## Do you want to help?

Test it and report issues. We've tested this on CouchBase Community 4.1.
You can also write more Unit Tests if you will.

# License

The [MIT](LICENSE) license.
