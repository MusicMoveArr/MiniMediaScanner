using MiniMediaScanner.Services;
using FluentAssertions;

namespace MiniMediaScanner.Tests.Services;

public class StringNormalizerServiceTests
{
    private readonly StringNormalizerService _stringNormalizerService = new StringNormalizerService();
    
    [Fact]
    public void RomanLettersShouldBeUppercased()
    {
        string romanLetters = "XXI XV:IV, X";
        string normalized = _stringNormalizerService.NormalizeText(romanLetters);
        normalized.Should().Be(romanLetters);
    }
    
    [Fact]
    public void EnDashShouldBeHyphen()
    {
        string enDashText = "Some – Dashes – don't like me";
        string hyphenText = "Some - Dashes - Don't Like Me";
        string normalized = _stringNormalizerService.NormalizeText(enDashText);
        normalized.Should().Be(hyphenText);
    }
    
    [Fact]
    public void EmDashShouldBeHyphen()
    {
        string enDashText = "Some — Dashes — don't like me";
        string hyphenText = "Some - Dashes - Don't Like Me";
        string normalized = _stringNormalizerService.NormalizeText(enDashText);
        normalized.Should().Be(hyphenText);
    }
    
    [Fact]
    public void HorizontalEllipsisShouldBeDots()
    {
        string ellipsisText = "Some … dots - confuse me";
        string dotsText = "Some ... Dots - Confuse Me";
        string normalized = _stringNormalizerService.NormalizeText(ellipsisText);
        normalized.Should().Be(dotsText);
    }
    
    [Fact]
    public void TestSongNames_1()
    {
        string songName = "XXI: Klavier";
        string songNameShouldBe = "XXI: Klavier";
        string normalized = _stringNormalizerService.NormalizeText(songName);
        normalized.Should().Be(songNameShouldBe);
    }
    
    [Fact]
    public void TestSongNames_2()
    {
        string songName = "2006-12-13: Live aus DisneyLand: Some Palace, St. Bla, Hmm";
        string songNameShouldBe = "2006-12-13: Live Aus Disneyland: Some Palace, St. Bla, Hmm";
        string normalized = _stringNormalizerService.NormalizeText(songName);
        normalized.Should().Be(songNameShouldBe);
    }
    
    [Fact]
    public void TestSongNames_3()
    {
        string songName = "Some & Love";
        string songNameShouldBe = "Some & Love";
        string normalized = _stringNormalizerService.NormalizeText(songName);
        normalized.Should().Be(songNameShouldBe);
    }
    
    [Fact]
    public void TestSongNames_4()
    {
        //small change in "Hip-hop" => "Hip-Hop"
        //small change in "vol" => "Vol"
        string songName = "Gold Collection - Hip-hop vol 1";
        string songNameShouldBe = "Gold Collection - Hip-Hop Vol 1";
        string normalized = _stringNormalizerService.NormalizeText(songName);
        normalized.Should().Be(songNameShouldBe);
    }
    
    [Fact]
    public void TestSongNames_5()
    {
        //small change in "remixed" => "Remixed"
        string songName = "Progressive Testing XXX (remixed)";
        string songNameShouldBe = "Progressive Testing XXX (Remixed)";
        string normalized = _stringNormalizerService.NormalizeText(songName);
        normalized.Should().Be(songNameShouldBe);
    }
    
    [Fact]
    public void TestSongNames_6()
    {
        //small changes in uppercase, greek language
        string songName = "Έι μαν κοίτα μπροστά";
        string songNameShouldBe = "Έι Μαν Κοίτα Μπροστά";
        string normalized = _stringNormalizerService.NormalizeText(songName);
        normalized.Should().Be(songNameShouldBe);
    }
    
    [Fact]
    public void TestSongNames_7()
    {
        //small changes in uppercase, greek language
        string songName = "The Archives: Demos & Other Fun Stuff!, Vol. 1";
        string songNameShouldBe = "The Archives: Demos & Other Fun Stuff!, Vol. 1";
        string normalized = _stringNormalizerService.NormalizeText(songName);
        normalized.Should().Be(songNameShouldBe);
    }
    
    [Fact]
    public void TestSongNames_8()
    {
        //small changes in uppercase
        string songName = "Some Song Name here [deluxe edition]";
        string songNameShouldBe = "Some Song Name Here [Deluxe Edition]";
        string normalized = _stringNormalizerService.NormalizeText(songName);
        normalized.Should().Be(songNameShouldBe);
    }
    
    [Fact]
    public void TestSongNames_9()
    {
        //small changes in uppercase
        string songName = "Can'T allOw WeIrD SPeLLinG";
        string songNameShouldBe = "Can't Allow Weird Spelling";
        string normalized = _stringNormalizerService.NormalizeText(songName);
        normalized.Should().Be(songNameShouldBe);
    }
    
    [Fact]
    public void TestAlbumNames_1()
    {
        //small difference in "Of" => "of"
        string songName = "The Best Of";
        string songNameShouldBe = "The Best of";
        string normalized = _stringNormalizerService.NormalizeText(songName);
        normalized.Should().Be(songNameShouldBe);
    }
}