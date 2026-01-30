using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Translate.Support;
using Translate.Utility;
using TriangleNet.Topology.DCEL;

namespace Translate.Tests;

public class GlossaryTests
{
    const string workingDirectory = "../../../../Files";

    [Fact]
    public void CleanupManualTranslations_RemovesEmptyEntries()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        var cache = new Dictionary<string, string>();
        var badDupes = new List<string>();
        var cleanedManuals = new List<GlossaryLine>();
        var cleanMe = new List<string>();

        foreach (var k in config.ManualTranslations)
        {
            if (cache.ContainsKey(k.Raw))
            {
                if (k.Result == cache[k.Raw])
                    cleanMe.Add(k.Raw);
                else
                    badDupes.Add(k.Raw);
            }
            else
            {
                cache.Add(k.Raw, k.Result);
                cleanedManuals.Add(k);
            }
        }

        if (badDupes.Count > 0)
        {
            File.WriteAllLines($"{workingDirectory}/TestResults/DupeBadGlossary.yaml", badDupes);
            throw new Exception($"Duplicate manual translations with different results found");
        }

        File.WriteAllLines($"{workingDirectory}/TestResults/DupeGlossary.yaml", cleanMe);

        //var serializer = Yaml.CreateSerializer();
        //var clean = serializer.Serialize(cleanedManuals);
        //File.WriteAllText($"{workingDirectory}/TestResults/CleanManualTranslations.yaml", clean);
    }
}
