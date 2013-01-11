using Ampere;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SamplePlugin
{
    public class TestProcessor
    {
        public IEnumerable<object> Run(BuildInstance instance, IEnumerable<object> inputs)
        {
            // parse the material as XML
            var stream = inputs.First() as Stream;
            if (stream == null)
            {
                instance.Log(LogLevel.Error, "Inputs to TestParser must be a single Stream object.");
                return null;
            }

            var document = XDocument.Load(stream);
            var root = document.Element("Test");
            if (root == null)
            {
                instance.Log(LogLevel.Error, "Test file must start with a <Test> node.");
                return null;
            }

            string data = null;
            var dataElement = root.Element("Data");
            if (dataElement != null)
                data = dataElement.Value;

            // example of finding all "dependencies" of this asset and kicking off builds for them
            var dependencies = root.Element("Dependencies");
            if (dependencies != null)
            {
                foreach (var element in dependencies.Elements())
                    instance.Start((string)element.Attribute("Name"));
            }

            // example of a "temporary" build, one that you can embed the results into this asset
            var embed = (string)root.Attribute("Embed");
            string embeddedData = null;

            if (embed != null)
            {
                var tempBuild = instance.StartTemp(embed);
                if (tempBuild == null)  // some error ocurred while trying to build it
                    return null;

                var tempStream = tempBuild.Results.First() as Stream;
                embeddedData = string.Concat(ToByteArray(tempStream));
            }

            var output = new MemoryStream();
            var writer = new StreamWriter(output) { AutoFlush = true };
            writer.WriteLine("This is my asset data! Woohoo!");
            if (data != null)
                writer.WriteLine(data);
            if (embeddedData != null)
                writer.WriteLine("Some embedded data: " + embeddedData);

            return new[] { output };
        }

        static byte[] ToByteArray(Stream stream)
        {
            var memStream = stream as MemoryStream;
            if (memStream != null)
                return memStream.ToArray();

            using (memStream = new MemoryStream())
            {
                stream.CopyTo(memStream);
                return memStream.ToArray();
            }
        }
    }
}
