using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Orleans;
using Orleans.Storage;
using Orleans.Runtime;
using Couchbase;
using System.Net;

namespace CouchBaseStorageTests
{
    public static class TaskExtensions
    {
        /// <summary>
        /// This will apply a timeout delay to the task, allowing us to exit early
        /// </summary>
        /// <param name="taskToComplete">The task we will timeout after timeSpan</param>
        /// <param name="timeout">Amount of time to wait before timing out</param>
        /// <exception cref="TimeoutException">If we time out we will get this exception</exception>
        /// <returns>The completed task</returns>
        internal static async Task WithTimeout(this Task taskToComplete, TimeSpan timeout)
        {
            if (taskToComplete.IsCompleted)
            {
                await taskToComplete;
                return;
            }

            var timeoutCancellationTokenSource = new CancellationTokenSource();
            var completedTask = await Task.WhenAny(taskToComplete, Task.Delay(timeout, timeoutCancellationTokenSource.Token));

            // We got done before the timeout, or were able to complete before this code ran, return the result
            if (taskToComplete == completedTask)
            {
                timeoutCancellationTokenSource.Cancel();
                // Await this so as to propagate the exception correctly
                await taskToComplete;
                return;
            }

            // We did not complete before the timeout, we fire and forget to ensure we observe any exceptions that may occur
            taskToComplete.Ignore();
            throw new TimeoutException(String.Format("WithTimeout has timed out after {0}.", timeout));
        }
    }
    public class CouchBaseMembershipDataManagerTests :IDisposable, IClassFixture<CouchBaseMembershipDataManagerTests.CouchBaseMembershipFixture>
    {
        private static readonly string hostName = Dns.GetHostName();

        public class CouchBaseMembershipFixture
        {
            public MembershipDataManager manager;

            public CouchBaseMembershipFixture()
            {
                var clientConfig = new Couchbase.Configuration.Client.ClientConfiguration();
                clientConfig.Servers.Clear();
                clientConfig.Servers.Add(new Uri("http://localhost:8091"));
                clientConfig.BucketConfigs.Clear();
                clientConfig.BucketConfigs.Add("membership", new Couchbase.Configuration.Client.BucketConfiguration
                {
                    BucketName = "membership",
                    Username = "",
                    Password = ""
                });
                manager = new MembershipDataManager("membership", clientConfig);
            }

            
        }

        private MembershipDataManager membershipTable;

        public CouchBaseMembershipDataManagerTests(CouchBaseMembershipFixture fixture)
        {
            membershipTable = fixture.manager;
        }

        [Fact]
        public async Task MembershipTable_GetGateways()
        {
            var membershipEntries = Enumerable.Range(0, 10).Select(i => CreateMembershipEntryForTest()).ToArray();

            membershipEntries[3].Status = SiloStatus.Active;
            membershipEntries[3].ProxyPort = 0;
            membershipEntries[5].Status = SiloStatus.Active;
            membershipEntries[9].Status = SiloStatus.Active;

            var data = await membershipTable.ReadAll();
            Assert.NotNull(data);
            Assert.Equal(0, data.Members.Count);

            var version = data.Version;
            foreach (var membershipEntry in membershipEntries)
            {
                Assert.True(await membershipTable.InsertRow(membershipEntry, version));
                version = (await membershipTable.ReadRow(membershipEntry.SiloAddress)).Version;
            }

            var gateways = await membershipTable.GetGateWays();

            var entries = new List<string>(gateways.Select(g => g.ToString()));

            Assert.True(entries.Contains(membershipEntries[5].SiloAddress.ToGatewayUri().ToString()));
            Assert.True(entries.Contains(membershipEntries[9].SiloAddress.ToGatewayUri().ToString()));
        }

        [Fact]
        public async Task MembershipTable_ReadAll_EmptyTable()
        {
            var data = await membershipTable.ReadAll();
            Assert.NotNull(data);

            //logger.Info("Membership.ReadAll returned VableVersion={0} Data={1}", data.Version, data);

            Assert.Equal(0, data.Members.Count);
            Assert.NotNull(data.Version.VersionEtag);
            Assert.Equal(0, data.Version.Version);
        }

        [Fact]
        public async Task MembershipTable_InsertRow()
        {
            var membershipEntry = CreateMembershipEntryForTest();

            var data = await membershipTable.ReadAll();
            Assert.NotNull(data);
            Assert.Equal(0, data.Members.Count);

            bool ok = await membershipTable.InsertRow(membershipEntry, data.Version.Next());
            Assert.True(ok, "InsertRow failed");

            data = await membershipTable.ReadAll();
            //since we don't support extended protocol
            Assert.Equal(0, data.Version.Version);
            Assert.Equal(1, data.Members.Count);
        }

        [Fact]
        public async Task MembershipTable_ReadRow_Insert_Read()
        {
            MembershipTableData data = await membershipTable.ReadAll();

            //logger.Info("Membership.ReadAll returned VableVersion={0} Data={1}", data.Version, data);

            Assert.Equal(0, data.Members.Count);

            TableVersion newTableVersion = data.Version;
            MembershipEntry newEntry = CreateMembershipEntryForTest();
            bool ok = await membershipTable.InsertRow(newEntry, newTableVersion);

            Assert.True(ok, "InsertRow failed");

            
            
            data = await membershipTable.ReadAll();
            Assert.Equal(0, data.Version.Version);//since we don't support extended protocol.

            var nextTableVersion = data.Version.Next();

            ok = await membershipTable.InsertRow(newEntry, nextTableVersion);
            Assert.False(ok, "InsertRow should have failed - duplicate entry");

            data = await membershipTable.ReadAll();

            Assert.Equal(1, data.Members.Count);

            data = await membershipTable.ReadRow(newEntry.SiloAddress);
            Assert.Equal(newTableVersion.Version, data.Version.Version);

            //logger.Info("Membership.ReadRow returned VableVersion={0} Data={1}", data.Version, data);

            Assert.Equal(1, data.Members.Count);

            Assert.NotNull(data.Version.VersionEtag);
            Assert.Equal(newTableVersion.VersionEtag, data.Version.VersionEtag);
            Assert.Equal(newTableVersion.Version, data.Version.Version);

            var membershipEntry = data.Members[0].Item1;
            string eTag = data.Members[0].Item2;
            //logger.Info("Membership.ReadRow returned MembershipEntry ETag={0} Entry={1}", eTag, membershipEntry);

            Assert.NotNull(eTag);
            Assert.NotNull(membershipEntry);
        }

        [Fact]
        public async Task MembershipTable_ReadAll_Insert_ReadAll()
        {
            MembershipTableData data = await membershipTable.ReadAll();
            //logger.Info("Membership.ReadAll returned VableVersion={0} Data={1}", data.Version, data);

            Assert.Equal(0, data.Members.Count);

            TableVersion newTableVersion = data.Version;
            MembershipEntry newEntry = CreateMembershipEntryForTest();
            bool ok = await membershipTable.InsertRow(newEntry, newTableVersion);

            Assert.True(ok, "InsertRow failed");

            data = await membershipTable.ReadAll();
            //logger.Info("Membership.ReadAll returned VableVersion={0} Data={1}", data.Version, data);

            Assert.Equal(1, data.Members.Count);

            Assert.NotNull(data.Version.VersionEtag);
            Assert.Equal(newTableVersion.VersionEtag, data.Version.VersionEtag);//since we don't support extended protocol

            Assert.Equal(newTableVersion.Version, data.Version.Version);

            var membershipEntry = data.Members[0].Item1;
            string eTag = data.Members[0].Item2;
            //logger.Info("Membership.ReadAll returned MembershipEntry ETag={0} Entry={1}", eTag, membershipEntry);

            Assert.NotNull(eTag);
            Assert.NotNull(membershipEntry);
        }

        [Fact]
        public async Task MembershipTable_UpdateRow()
        {
            var tableData = await membershipTable.ReadAll();

            Assert.NotNull(tableData.Version);
            Assert.Equal(0, tableData.Version.Version);
            Assert.Equal(0, tableData.Members.Count);

            for (int i = 1; i < 10; i++)
            {
                var siloEntry = CreateMembershipEntryForTest();

                siloEntry.SuspectTimes =
                    new List<Tuple<SiloAddress, DateTime>>
                    {
                        new Tuple<SiloAddress, DateTime>(CreateSiloAddressForTest(), GetUtcNowWithSecondsResolution().AddSeconds(1)),
                        new Tuple<SiloAddress, DateTime>(CreateSiloAddressForTest(), GetUtcNowWithSecondsResolution().AddSeconds(2))
                    };

                TableVersion tableVersion = tableData.Version;
                //logger.Info("Calling InsertRow with Entry = {0} TableVersion = {1}", siloEntry, tableVersion);
                bool ok = await membershipTable.InsertRow(siloEntry, tableVersion);

                Assert.True(ok, "InsertRow failed");

                tableData = await membershipTable.ReadAll();

                var etagBefore = tableData.Get(siloEntry.SiloAddress).Item2;

                Assert.NotNull(etagBefore);

                //logger.Info("Calling UpdateRow with Entry = {0} correct eTag = {1} old version={2}", siloEntry,
                //  etagBefore, tableVersion);

                ok = await membershipTable.UpdateRow(siloEntry, tableVersion, etagBefore);

                Assert.False(ok, $"row update should have failed - Table Data = {tableData}");

                tableData = await membershipTable.ReadAll();

                tableVersion = tableData.Version;

                //logger.Info("Calling UpdateRow with Entry = {0} correct eTag = {1} correct version={2}", siloEntry,
                //etagBefore, tableVersion);
                ok = await membershipTable.UpdateRow(siloEntry, tableVersion, etagBefore);

                Assert.True(ok, $"UpdateRow failed - Table Data = {tableData}");

                //logger.Info("Calling UpdateRow with Entry = {0} old eTag = {1} old version={2}", siloEntry,
                //  etagBefore, tableVersion);

                ok = await membershipTable.UpdateRow(siloEntry, tableVersion, etagBefore);

                Assert.False(ok, $"row update should have failed - Table Data = {tableData}");

                tableData = await membershipTable.ReadAll();

                var tuple = tableData.Get(siloEntry.SiloAddress);

                //##Assert.Equal(tuple.Item1.ToFullString(true), siloEntry.ToFullString(true));

                var etagAfter = tuple.Item2;

                //logger.Info("Calling UpdateRow with Entry = {0} correct eTag = {1} old version={2}", siloEntry,
                //  etagAfter, tableVersion);

                ok = await membershipTable.UpdateRow(siloEntry, tableVersion, etagAfter);

                Assert.False(ok, $"row update should have failed - Table Data = {tableData}");

                //var nextTableVersion = tableData.Version.Next();

                //logger.Info("Calling UpdateRow with Entry = {0} old eTag = {1} correct version={2}", siloEntry,
                //    etagBefore, nextTableVersion);

                //ok = await membershipTable.UpdateRow(siloEntry, etagBefore, nextTableVersion);

                //Assert.False(ok, $"row update should have failed - Table Data = {tableData}");

                tableData = await membershipTable.ReadAll();

                etagBefore = etagAfter;

                etagAfter = tableData.Get(siloEntry.SiloAddress).Item2;

                Assert.Equal(etagBefore, etagAfter);
                Assert.NotNull(tableData.Version);
                Assert.Equal(tableVersion.Version, tableData.Version.Version);
                Assert.Equal(i, tableData.Members.Count);
            }
        }

        [Fact]
        public async Task MembershipTable_UpdateRowInParallel()
        {
            var tableData = await membershipTable.ReadAll();

            var data = CreateMembershipEntryForTest();

            var newTableVer = tableData.Version.Next();

            var insertions = Task.WhenAll(Enumerable.Range(1, 20).Select(i => membershipTable.InsertRow(data, newTableVer)));

            Assert.True((await insertions).Single(x => x), "InsertRow failed");

            await Task.WhenAll(Enumerable.Range(1, 19).Select(async i =>
            {
                bool done;
                do
                {
                    var updatedTableData = await membershipTable.ReadAll();
                    var updatedRow = updatedTableData.Get(data.SiloAddress);
                    var tableVersion = updatedTableData.Version.Next();
                    await Task.Delay(10);
                    done = await membershipTable.UpdateRow(updatedRow.Item1, tableVersion, updatedRow.Item2);
                } while (!done);
            })).WithTimeout(TimeSpan.FromSeconds(30));


            tableData = await membershipTable.ReadAll();
            Assert.NotNull(tableData.Version);
            Assert.Equal(20, tableData.Version.Version);
            Assert.Equal(1, tableData.Members.Count);
        }


        private static int generation;
        // Utility methods
        private static MembershipEntry CreateMembershipEntryForTest()
        {
            SiloAddress siloAddress = CreateSiloAddressForTest();


            var membershipEntry = new MembershipEntry
            {
                SiloAddress = siloAddress,
                HostName = hostName,
                InstanceName = "TestSiloName",
                Status = SiloStatus.Joining,
                ProxyPort = siloAddress.Endpoint.Port,
                StartTime = GetUtcNowWithSecondsResolution(),
                IAmAliveTime = GetUtcNowWithSecondsResolution()
            };

            return membershipEntry;
        }

        private static DateTime GetUtcNowWithSecondsResolution()
        {
            var now = DateTime.UtcNow;
            return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
        }

        private static SiloAddress CreateSiloAddressForTest()
        {
            var siloAddress = SiloAddress.NewLocalAddress(Interlocked.Increment(ref generation));
            siloAddress.Endpoint.Port = 12345;
            return siloAddress;
        }

        public void Dispose()
        {
            if (membershipTable != null)
                membershipTable.DeleteMembershipTableEntries("").Wait();
        }
    }
}