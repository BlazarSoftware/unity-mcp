using Newtonsoft.Json.Linq;
using NUnit.Framework;
using MCPForUnity.Editor.Tools;
using static MCPForUnityTests.Editor.TestUtilities;

namespace MCPForUnityTests.Editor.Tools
{
    public class ManageAddressablesTests
    {
        [Test]
        public void CheckAddressablesPackage_ReturnsPackageStatus()
        {
            // Act
            var result = ToJObject(ManageAddressables.HandleCommand(new JObject
            {
                ["action"] = "check_package"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["hasAddressables"]);
            Assert.IsNotNull(data["recommendation"]);
        }
    }
}
