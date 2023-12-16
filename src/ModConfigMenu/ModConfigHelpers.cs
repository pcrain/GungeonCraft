namespace CwaffingTheGungy;

public static class ModConfigHelpers
{
  public const string MARKUP_DELIM = "@"; // "#" is used for localization strings, so we need something else

  // Add color formatting to a string for use within option menus
  public static string WithColor(this string s, Color c)
  {
    return $"{MARKUP_DELIM}{ColorUtility.ToHtmlStringRGB(c)}{s}";
  }

  // Basic colorizers
  public static string Red(this string s)         => s.WithColor(Color.red);
  public static string Green(this string s)       => s.WithColor(Color.green);
  public static string Blue(this string s)        => s.WithColor(Color.blue);
  public static string Yellow(this string s)      => s.WithColor(Color.yellow);
  public static string Cyan(this string s)        => s.WithColor(Color.cyan);
  public static string Magenta(this string s)     => s.WithColor(Color.magenta);
  public static string Gray(this string s)        => s.WithColor(Color.gray);
  public static string White(this string s)       => s.WithColor(Color.white);
  public static Color Dim(this Color c, bool dim) => Color.Lerp(dim ? Color.black : Color.white, c, 0.5f);

  // Helpers for processing colors on various dfControls
  internal static string ProcessColors(this string markupText, out Color color)
  {
    string processedText = markupText;
    color = Color.white;
    if (processedText.StartsWith(MARKUP_DELIM))
    {
      // convert "@" back to "#" for the purposes of color conversion
      if (ColorUtility.TryParseHtmlString($"#{processedText.Substring(1, 6)}", out color))
        processedText = processedText.Substring(7);
      else
        color = Color.white;
    }
    return processedText;
  }
}
