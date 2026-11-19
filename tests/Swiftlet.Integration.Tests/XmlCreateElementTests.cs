using System.Xml;
using Swiftlet.Runtime;

internal static class XmlCreateElementTests
{
    public static async Task RunAsync()
    {
        var a = new XmlDocument();
        var b = new XmlDocument();
        using var rendezvous = new Barrier(2);
        int heldAtSecondSource = 0;

        XmlNode[] Inputs(bool reverse)
        {
            XmlDocument first = reverse ? b : a, second = reverse ? a : b;
            // Each worker reaches its second source at the same time. A recursive
            // lock implementation still owns its first source here and deadlocks.
            var left = new ObservedElement(reverse ? "b" : "a", first);
            var right = new ObservedElement(reverse ? "a" : "b", second, () =>
            {
                if (Monitor.IsEntered(first)) Interlocked.Increment(ref heldAtSecondSource);
                if (!rendezvous.SignalAndWait(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("The second XML import was not reached by both workers.");
            });
            left.InnerXml = "<nested key=\"value\">text</nested>";
            right.InnerXml = "<nested key=\"value\">text</nested>";
            return [left, right];
        }

        XmlNode[] forward = Inputs(false), backward = Inputs(true);
        foreach (ObservedElement node in forward.Concat(backward)) node.Observe = true;
        Task<XmlElement> Start(XmlNode[] nodes) => Task.Factory.StartNew(
            () => Build(nodes), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        XmlElement[] results;
        try
        {
            results = await Task.WhenAll(Start(forward), Start(backward))
                .WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (TimeoutException ex)
        {
            throw new TimeoutException($"XML imports did not finish; {heldAtSecondSource} workers held their first document at the second import.", ex);
        }
        Assert.Equal(0, heldAtSecondSource);
        Assert.Equal("ab", string.Concat(results[0].ChildNodes.Cast<XmlNode>().Select(n => n.Name)));
        Assert.Equal("ba", string.Concat(results[1].ChildNodes.Cast<XmlNode>().Select(n => n.Name)));
        foreach (XmlElement result in results)
        {
            Assert.Equal("urn:test", result.NamespaceURI);
            Assert.Equal("ok", result.GetAttribute("status"));
            Assert.Equal(2, result.SelectNodes("*/nested[@key='value']")!.Count);
            foreach (XmlNode child in result.ChildNodes)
                Assert.True(ReferenceEquals(result.OwnerDocument, child.OwnerDocument));
        }
        foreach (ObservedElement node in forward.Concat(backward))
        {
            node.Observe = false;
            Assert.Null(node.ParentNode);
            Assert.Equal("<nested key=\"value\">text</nested>", node.InnerXml);
        }

        // Duplicate references, empty input and skipped attribute diagnostics keep
        // their existing semantics; imported nodes must be independent copies.
        XmlElement repeated = XmlQueries.CreateElement("root", "prefix", null,
            ["", "status"], ["ignored", "ok"], [forward[0], null, forward[0]], out var skipped);
        Assert.Equal(1, skipped.Count); Assert.Equal(0, skipped[0]);
        Assert.Equal(3, repeated.ChildNodes.Count);
        Assert.True(!ReferenceEquals(repeated.ChildNodes[1], repeated.ChildNodes[2]));
        ((XmlElement)repeated.ChildNodes[1]!).SetAttribute("changed", "yes");
        Assert.Equal("", ((XmlElement)repeated.ChildNodes[2]!).GetAttribute("changed"));
        Assert.Equal("<empty />", XmlQueries.CreateElement("empty", null, null,
            [], [], [], out _).OuterXml);
    }

    private static XmlElement Build(XmlNode[] nodes) => XmlQueries.CreateElement(
        "root", null, "urn:test", ["status"], ["ok"], nodes, out _);

    private sealed class ObservedElement : XmlElement
    {
        private readonly XmlDocument source;
        private Action? firstOwnerRead;
        public bool Observe { get; set; }

        public ObservedElement(string name, XmlDocument source, Action? firstOwnerRead = null)
            : base("", name, "", source)
        {
            this.source = source;
            this.firstOwnerRead = firstOwnerRead;
        }

        public override XmlDocument OwnerDocument
        {
            get
            {
                if (Observe) Interlocked.Exchange(ref firstOwnerRead, null)?.Invoke();
                return source;
            }
        }

        public override string LocalName
        {
            get
            {
                if (Observe && !Monitor.IsEntered(source))
                    throw new InvalidOperationException("XML import read the source without its document lock.");
                return base.LocalName;
            }
        }
    }
}
