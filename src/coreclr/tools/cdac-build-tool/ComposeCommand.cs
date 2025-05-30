// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

internal sealed class ComposeCommand : Command
{
    private readonly Argument<string[]> inputFiles = new("INPUT [INPUTS...]") { Arity = ArgumentArity.OneOrMore, Description = "One or more input files" };
    private readonly Option<string> outputFile = new("-o") { Arity = ArgumentArity.ExactlyOne, HelpName = "OUTPUT", Required = true, Description = "Output file" };
    private readonly Option<string[]> contractFile = new("-c") { Arity = ArgumentArity.ZeroOrMore, HelpName = "CONTRACT", Description = "Contract file (may be specified multiple times)" };
    private readonly Option<string> baselinePath = new("-b", "--baseline") { Arity = ArgumentArity.ExactlyOne, HelpName = "BASELINEPATH", Description = "Directory containing the baseline contracts"};
    private readonly Option<string> templateFile = new("-i", "--input-template") { Arity = ArgumentArity.ExactlyOne, HelpName = "TEMPLATE", Description = "Contract descriptor template to be filled in" };
    private readonly Option<bool> _verboseOption;
    public ComposeCommand(Option<bool> verboseOption) : base("compose")
    {
        _verboseOption = verboseOption;
        Add(inputFiles);
        Add(outputFile);
        Add(contractFile);
        Add(baselinePath);
        Add(templateFile);
        SetAction(Run);
    }

    private async Task<int> Run(ParseResult parse, CancellationToken token = default)
    {
        var inputs = parse.GetValue(inputFiles);
        if (inputs == null || inputs.Length == 0)
        {
            Console.Error.WriteLine("No input files specified");
            return 1;
        }
        var output = parse.GetValue(outputFile);
        if (output == null)
        {
            Console.Error.WriteLine("No output file specified");
            return 1;
        }
        var baselinesDir = parse.GetValue(baselinePath);
        if (baselinesDir == null)
        {
            Console.Error.WriteLine("No baseline path specified");
            return 1;
        }
        baselinesDir = System.IO.Path.GetFullPath(baselinesDir);
        if (!System.IO.Directory.Exists(baselinesDir))
        {
            Console.Error.WriteLine($"Baseline path {baselinesDir} does not exist");
            return 1;
        }
        var templateFilePath = parse.GetValue(templateFile);
        if (templateFilePath == null)
        {
            Console.Error.WriteLine("No template file specified");
            return 1;
        }
        templateFilePath = System.IO.Path.GetFullPath(templateFilePath);
        if (!System.IO.File.Exists(templateFilePath))
        {
            Console.Error.WriteLine($"Template file {templateFilePath} does not exist");
            return 1;
        }
        var contracts = parse.GetValue(contractFile);
        var verbose = parse.GetValue(_verboseOption);
        var builder = new DataDescriptorModel.Builder(baselinesDir);
        var scraper = new ObjectFileScraper(verbose, builder);
        foreach (var input in inputs)
        {
            token.ThrowIfCancellationRequested();
            if (!await scraper.ScrapeInput(input, token).ConfigureAwait(false))
            {
                Console.Error.WriteLine($"could not scrape payload in {input}");
                return 1;
            }
        }
        if (contracts != null)
        {
            var contractReader = new ContractReader(builder);
            foreach (var contract in contracts)
            {
                if (!await contractReader.ParseContracts(contract, token).ConfigureAwait(false))
                {
                    Console.Error.WriteLine($"could not parse contracts in {contract}");
                    return 1;
                }
            }
        }

        var model = builder.Build();
        if (verbose)
        {
            model.DumpModel();
        }
        EnsureDirectoryExists(output);
        using var writer = new System.IO.StreamWriter(output);
        var emitter = new ContractDescriptorSourceFileEmitter(templateFilePath);
        emitter.SetPlatformFlags(model.PlatformFlags);
        emitter.SetPointerDataCount(model.PointerDataCount);
        emitter.SetJsonDescriptor(model.ToJson());
        emitter.Emit(writer);
        await writer.FlushAsync(token).ConfigureAwait(false);
        return 0;
    }

    private static void EnsureDirectoryExists(string outputPath)
    {
        var directory = System.IO.Path.GetDirectoryName(outputPath);
        if (directory == null)
        {
            return;
        }
        System.IO.Directory.CreateDirectory(directory);
    }
}
