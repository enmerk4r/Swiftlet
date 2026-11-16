using Grasshopper2.Doc.Migration;

namespace Swiftlet.Gh2.Migration;

// Grasshopper 1 -> Grasshopper 2 rules for "5. Read HTML" and "6. Read XML". All of these components
// are stateless and keep their pin order, so identity rules (GH1 guid, GH2 id, inputs, outputs) suffice.
public sealed class MarkupMigrationTable : IMigrationTable
{
    public IEnumerable<IMigrateBase> CreateMigrators()
    {
        yield return Gh1MigrationRule.Identity("82d23920-743c-41b8-b5ff-085cb3f6f086", ComponentIds.GetHtmlAttributeValue, 2, 1); // Get Attribute Value
        yield return Gh1MigrationRule.Identity("2e2fbf29-4cce-4ee2-9055-2a3d7f3a60fe", ComponentIds.GetHtmlNodeChildren, 1, 1); // Get Child Nodes
        yield return Gh1MigrationRule.Identity("799427c4-0e1b-4adb-b3a0-1ce33ddf7a41", ComponentIds.GetElementById, 2, 1); // Get Element By Id
        yield return Gh1MigrationRule.Identity("560a0b7b-8c6a-4299-9433-6022657d178c", ComponentIds.GetElementsByAttributeValue, 4, 1); // Get Elements By Attribute Value
        yield return Gh1MigrationRule.Identity("7d89f228-1fdd-4b38-81da-ece9c8634a74", ComponentIds.GetElementsByClassName, 3, 1); // Get Elements By Class Name
        yield return Gh1MigrationRule.Identity("42ea89c9-46bd-4c81-8553-e0fd11cb652a", ComponentIds.GetElementsByTagName, 3, 1); // Get Elements By Tag Name
        yield return Gh1MigrationRule.Identity("dced6d9c-0654-484a-aec3-02d941f22ea9", ComponentIds.GetElementsByXPath, 2, 1); // Get Elements By XPATH
        yield return Gh1MigrationRule.Identity("08d3c43f-1537-4c91-9cc0-d429ff62a89c", ComponentIds.GetHtmlAttributes, 1, 2); // Get Html Attributes
        yield return Gh1MigrationRule.Identity("155b7526-e8ff-494e-81d7-a9bbee72fc5b", ComponentIds.GetInnerHtml, 1, 1); // Get Inner HTML
        yield return Gh1MigrationRule.Identity("349b64d0-63e5-4263-85e1-788d03a71920", ComponentIds.ReadHtml, 1, 1); // Read HTML
        yield return Gh1MigrationRule.Identity("48819932-f530-4764-b9fa-fb44998cf11f", ComponentIds.StringifyHtmlNode, 1, 1); // Stringify HTML Node
        yield return Gh1MigrationRule.Identity("d4e5f6a7-b8c9-7d8e-2f3a-4b5c6d7e8f90", ComponentIds.GetXmlAttributeValue, 2, 1); // Get XML Attribute Value
        yield return Gh1MigrationRule.Identity("e5f6a7b8-c9d0-8e9f-3a4b-5c6d7e8f9001", ComponentIds.GetXmlAttributes, 1, 2); // Get XML Attributes
        yield return Gh1MigrationRule.Identity("a7b8c9d0-e1f2-0011-5c6d-7e8f90011223", ComponentIds.GetXmlChildNodes, 1, 1); // Get XML Child Nodes
        yield return Gh1MigrationRule.Identity("b2c3d4e5-f6a7-5b6c-0d1e-2f3a4b5c6d7e", ComponentIds.GetXmlElementsByTag, 2, 1); // Get XML Elements By Tag
        yield return Gh1MigrationRule.Identity("c3d4e5f6-a7b8-6c7d-1e2f-3a4b5c6d7e8f", ComponentIds.GetXmlElementsByXPath, 2, 1); // Get XML Elements By XPath
        yield return Gh1MigrationRule.Identity("f6a7b8c9-d0e1-9f00-4b5c-6d7e8f900112", ComponentIds.GetXmlInnerText, 1, 2); // Get XML Inner Text
        yield return Gh1MigrationRule.Identity("a1b2c3d4-e5f6-4a5b-9c0d-1e2f3a4b5c6d", ComponentIds.ParseXml, 1, 1); // Parse XML
        yield return Gh1MigrationRule.Identity("b8c9d0e1-f2a3-1122-6d7e-8f9001122334", ComponentIds.StringifyXmlNode, 2, 1); // Stringify XML Node
        yield return Gh1MigrationRule.Identity("f7e8d9c0-b1a2-4c3d-8e5f-6a7b8c9d0e1f", ComponentIds.CreateXmlElement, 6, 1); // Create XML Element
    }
}
