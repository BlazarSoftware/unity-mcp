using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace MCPForUnity.Editor.Tests.Tools
{
    [TestFixture]
    public class ManagePackagesTests
    {
        [Test]
        public void HandleCommand_Ping_ReturnsSuccess()
        {
            var @params = new JObject { ["action"] = "ping" };

            var result = ManagePackages.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var success = (SuccessResponse)result;
            Assert.AreEqual("pong", success.Message);
        }

        [Test]
        public void HandleCommand_MissingAction_ReturnsError()
        {
            var @params = new JObject();

            var result = ManagePackages.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void HandleCommand_UnknownAction_ReturnsError()
        {
            var @params = new JObject { ["action"] = "unknown_action" };

            var result = ManagePackages.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void List_ReturnsPackages()
        {
            var @params = new JObject
            {
                ["action"] = "list",
                ["offlineMode"] = true
            };

            var result = ManagePackages.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void Add_MissingPackageId_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "add"
            };

            var result = ManagePackages.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void Remove_MissingPackageName_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "remove"
            };

            var result = ManagePackages.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void Search_AllPackages_ReturnsResults()
        {
            var @params = new JObject
            {
                ["action"] = "search",
                ["offlineMode"] = true
            };

            var result = ManagePackages.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void GetInfo_MissingPackageName_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "get_info"
            };

            var result = ManagePackages.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void GetInfo_NonExistentPackage_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "get_info",
                ["name"] = "com.nonexistent.package.that.does.not.exist"
            };

            var result = ManagePackages.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void Embed_MissingPackageName_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "embed"
            };

            var result = ManagePackages.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void Resolve_ReturnsSuccess()
        {
            var @params = new JObject
            {
                ["action"] = "resolve"
            };

            var result = ManagePackages.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }
    }
}
