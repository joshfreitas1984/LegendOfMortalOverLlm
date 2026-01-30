//using SharedAssembly.TextResizer;

//namespace Translate.Tests;

//public class WildcardMatchingServiceTests
//{
//    private readonly WildcardMatchingService _wildcardMatcher;

//    public WildcardMatchingServiceTests()
//    {
//        // Setup test contracts
//        List<TextResizerContract> contracts = new List<TextResizerContract>
//        {
//            new TextResizerContract { Path = @"\Path\s*ab\" },
//            new TextResizerContract { Path = @"\Path\s\*\cd\" },
//            new TextResizerContract { Path = @"\GameStart\Some\Path\*\rest\Should\Match\*\text" },
//            new TextResizerContract { Path = @"\Simple\Path\NoWildcard\" }
//        };

//        _wildcardMatcher = new WildcardMatchingService(contracts);
//    }

//    [Theory]
//    [InlineData(@"\GameStart\Some\Path\whatever\rest\Should\Match\anything\text", @"\GameStart\Some\Path\*\rest\Should\Match\*\text")]
//    [InlineData(@"\Simple\Path\NoWildcard\", @"\Simple\Path\NoWildcard\")]
//    [InlineData(@"\Path\s\very\very\long\path\with\many\segments\cd\", @"\Path\s\*\cd\")]
//    [InlineData(@"\Path\slab\", @"\Path\s*ab\")]
//    [InlineData(@"\Path\sab\", @"\Path\s*ab\")]
//    [InlineData(@"\Path\s\1\2\3\ab\", @"\Path\s*ab\")]
//    [InlineData(@"\Path\s\very\very\long\path\with\many\segments\ab\", @"\Path\s*ab\")]
//    public void FindMatch_WithValidPaths_ReturnsCorrectContract(string inputPath, string expectedContractPath)
//    {
//        // Act
//        var result = _wildcardMatcher.FindMatch(inputPath);

//        // Assert
//        Assert.Equal(expectedContractPath, result?.Path);
//    }

//    [Theory]
//    [InlineData(@"\Path\nomatch\")]
//    [InlineData(@"\GameStart\Some\Path\missing\part\text")]
//    [InlineData(@"\Simple\Different\Path\")]
//    public void FindMatch_WithNonMatchingPaths_ReturnsNull(string inputPath)
//    {
//        // Act
//        var result = _wildcardMatcher.FindMatch(inputPath);

//        // Assert
//        Assert.Null(result);
//    }   
//}