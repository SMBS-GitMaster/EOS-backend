using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Utilities.Reports {

  public interface IPallet {
    string GetColor(double percent);
    string NextColor();
    int GetColorCount();
  }

  public class Pallets {
    public static IPallet Stratified = new StratifiedPallet();
    public static IPallet Scale = new ScalePallet();
    public static IPallet RedWhiteGreen = new RedWhiteGreen();
    public static IPallet GreenWhiteRed = new GreenWhiteRed();
    public static IPallet Orange = new OrangePallet(false);
    public static IPallet OrangeDesc = new OrangePallet(true);
    public static IPallet GrayScale = new GrayPallet(true);
    public static IPallet RedYellowGreen = new RedYellowGreen();
    public static IPallet PinkRedYellowGreenBlue = new PinkRedYellowGreenBlue();
  }

  public abstract class InterpolatePallet : IPallet {
    protected abstract string[] GetColors();

    public string GetColor(double percent) {
      if (double.IsNaN(percent))
        percent = .5;

      var Colors = GetColors();
      percent = Math.Max(0, Math.Min(1, percent));
      var idx = (int)Math.Round((Colors.Count() - 1) * percent);
      return Colors[idx];
    }

    protected int colorIndex = 0;
    public string NextColor() {
      var colors = GetColors();
      return colors[colorIndex++ % colors.Length];
    }

    public int GetColorCount() {
      return GetColors().Count();
    }
  }
  public class GreenWhiteRed : InterpolatePallet {
    protected override string[] GetColors() {
      return new string[] { "#2e7d32", "#358037", "#3b843c", "#418741", "#478a46", "#4c8d4b", "#529150", "#579455", "#5d975a", "#629a5f", "#679e64", "#6da169", "#72a46e", "#77a873", "#7cab78", "#81ae7d", "#87b282", "#8cb587", "#91b88d", "#96bc92", "#9bbf97", "#a0c29c", "#a6c6a2", "#abc9a7", "#b0ccac", "#b5d0b2", "#bad3b7", "#c0d6bc", "#c5dac2", "#caddc7", "#cfe0cd", "#d5e4d2", "#dae7d8", "#dfebdd", "#e4eee3", "#eaf1e8", "#eff5ee", "#f4f8f4", "#fafcf9", "#ffffff", "#fffaf8", "#fff4f1", "#ffefea", "#ffeae3", "#ffe4dd", "#ffdfd6", "#ffdacf", "#ffd4c8", "#ffcfc2", "#ffcabb", "#ffc4b4", "#ffbfae", "#ffbaa7", "#ffb4a1", "#ffaf9a", "#feaa94", "#fda48d", "#fc9f87", "#fb9981", "#fa947a", "#f98f74", "#f7896e", "#f68368", "#f47e62", "#f3785b", "#f17255", "#ef6d4f", "#ee6749", "#ec6143", "#ea5b3d", "#e85437", "#e64e31", "#e3472b", "#e13f25", "#df381e", "#dc2f18", "#da2510", "#d81708", "#d50000" };
    }
  }

  public class RedYellowGreen : InterpolatePallet {
    protected override string[] GetColors() {
      return new string[] { "#ff0000", "#d50000", "#db2800", "#e13e00", "#e74f00", "#eb5f00", "#f06f00", "#f37d00", "#f78b00", "#f99900", "#fca700", "#fdb400", "#fec200", "#ffcf00", "#ffdd00", "#ffea00", "#ffea00", "#ede306", "#dbdc0d", "#cad514", "#b9ce19", "#a9c61e", "#99bf22", "#8ab726", "#7caf29", "#6ea72c", "#609e2e", "#539630", "#478e31", "#3a8532", "#2e7d32", "#00ff00" };
    }
  }
  public class PinkRedYellowGreenBlue : InterpolatePallet {
    protected override string[] GetColors() {
      return new string[] { "#ff00ff", "#d50000", "#db2800", "#e13e00", "#e74f00", "#eb5f00", "#f06f00", "#f37d00", "#f78b00", "#f99900", "#fca700", "#fdb400", "#fec200", "#ffcf00", "#ffdd00", "#ffea00", "#ffea00", "#ede306", "#dbdc0d", "#cad514", "#b9ce19", "#a9c61e", "#99bf22", "#8ab726", "#7caf29", "#6ea72c", "#609e2e", "#539630", "#478e31", "#3a8532", "#2e7d32", "#0000ff" };
    }
  }

  public class GrayPallet : InterpolatePallet {
    public string[] colors { get; private set; }
    public GrayPallet(bool desc) {
      var c = new string[] { "#222222", "#282828", "#2f2f2f", "#363636", "#3d3d3d", "#444444", "#4b4b4b", "#525252", "#5a5a5a", "#616161", "#696969", "#717171", "#797979", "#818181", "#898989", "#919191", "#999999", "#a1a1a1", "#a9a9a9", "#b2b2b2", "#bababa", "#c3c3c3", "#cccccc", "#d4d4d4", "#dddddd" };
      if (desc)
        colors = c.Reverse().ToArray();
      else
        colors = c;
    }


    protected override string[] GetColors() {
      return colors;
    }

  }

  public class RedWhiteGreen : InterpolatePallet {
    protected override string[] GetColors() {
      return new string[] { "#d50000", "#d81708", "#da2510", "#dc2f18", "#df381e", "#e13f25", "#e3472b", "#e64e31", "#e85437", "#ea5b3d", "#ec6143", "#ee6749", "#ef6d4f", "#f17255", "#f3785b", "#f47e62", "#f68368", "#f7896e", "#f98f74", "#fa947a", "#fb9981", "#fc9f87", "#fda48d", "#feaa94", "#ffaf9a", "#ffb4a1", "#ffbaa7", "#ffbfae", "#ffc4b4", "#ffcabb", "#ffcfc2", "#ffd4c8", "#ffdacf", "#ffdfd6", "#ffe4dd", "#ffeae3", "#ffefea", "#fff4f1", "#fffaf8", "#ffffff", "#fafcf9", "#f4f8f4", "#eff5ee", "#eaf1e8", "#e4eee3", "#dfebdd", "#dae7d8", "#d5e4d2", "#cfe0cd", "#caddc7", "#c5dac2", "#c0d6bc", "#bad3b7", "#b5d0b2", "#b0ccac", "#abc9a7", "#a6c6a2", "#a0c29c", "#9bbf97", "#96bc92", "#91b88d", "#8cb587", "#87b282", "#81ae7d", "#7cab78", "#77a873", "#72a46e", "#6da169", "#679e64", "#629a5f", "#5d975a", "#579455", "#529150", "#4c8d4b", "#478a46", "#418741", "#3b843c", "#358037", "#2e7d32" };
    }
  }

  public class ScalePallet : InterpolatePallet {
    protected override string[] GetColors() {
      return new string[] { "#5C2D2B", "#5E2F2E", "#603031", "#623133", "#643236", "#663339", "#68353C", "#6A363F", "#6B3742", "#6D3945", "#6E3B48", "#6F3C4B", "#713E4E", "#724051", "#734155", "#734358", "#74455B", "#75475E", "#754962", "#754B65", "#764D68", "#76506C", "#76526F", "#755472", "#755775", "#745978", "#745B7B", "#735E7E", "#726081", "#716384", "#6F6587", "#6E688A", "#6C6A8C", "#6A6D8F", "#696F91", "#677294", "#657596", "#627798", "#607A9A", "#5D7C9C", "#5B7F9E", "#58819F", "#5684A1", "#5387A2", "#5089A3", "#4D8CA4", "#4A8EA5", "#4791A6", "#4493A6", "#4296A7", "#3F98A7", "#3C9AA7", "#3A9DA7", "#389FA7", "#36A2A6", "#35A4A6", "#34A6A5", "#34A9A5", "#34ABA4", "#35ADA3", "#36AFA1", "#38B1A0", "#3AB39F", "#3DB69D", "#40B89B", "#44BA9A", "#47BC98", "#4CBE96", "#50C094", "#55C192", "#59C38F", "#5EC58D", "#63C78B", "#68C989", "#6ECA86", "#73CC84", "#79CD81", "#7ECF7F", "#84D07D", "#8AD27A", "#90D378", "#96D576", "#9CD673", "#A2D771", "#A8D86F", "#AED96D", "#B4DA6B", "#BBDB69", "#C1DC68", "#C8DD66", "#CEDE65", "#D5DF63", "#DBDF62", "#E2E062" };
    }
  }

  public class OrangePallet : InterpolatePallet {
    public string[] colors { get; private set; }
    public OrangePallet(bool desc) {
      var c = new[] { "#F95003", "#F65004", "#F35005", "#F14F07", "#EE4F08", "#EC4F09", "#E94F0A", "#E74E0B", "#E44E0C", "#E24E0D", "#DF4D0E", "#DD4D0F", "#DA4D0F", "#D84C10", "#D54C11", "#D34B11", "#D04B12", "#CE4B12", "#CC4A13", "#C94A13", "#C74914", "#C44914", "#C24915", "#C04815", "#BD4815", "#BB4716", "#B94716", "#B64616", "#B44616", "#B24517", "#AF4517", "#AD4417", "#AB4417", "#A94317", "#A64317", "#A44218", "#A24118", "#A04118", "#9E4018", "#9B4018", "#993F18", "#973F18", "#953E18", "#933D18", "#913D18", "#8E3C18", "#8C3C18", "#8A3B18", "#883A18", "#863A18", "#843918", "#823818", "#803818", "#7E3718", "#7C3617", "#7A3617", "#783517", "#763517", "#743417", "#723317", "#703217", "#6E3217", "#6C3116", "#6A3016", "#683016", "#662F16", "#642E16", "#622E15", "#602D15", "#5F2C15", "#5D2C15", "#5B2B14", "#592A14", "#572914", "#552914", "#542813", "#522713", "#502713", "#4E2612", "#4D2512", "#4B2412", "#492412", "#472311", "#462211", "#442111", "#422110", "#412010", "#3F1F10" };
      if (desc)
        colors = c.Reverse().ToArray();
      else
        colors = c;
    }


    protected override string[] GetColors() {
      return colors;
    }
  }

  public class StratifiedPallet : InterpolatePallet {
    protected override string[] GetColors() {
      return new string[] { "#f23d3d", "#e5b073", "#3df2ce", "#c200f2", "#e57373", "#f2b63d", "#3de6f2", "#e639c3", "#ff2200", "#d9d26c", "#0099e6", "#d9368d", "#d96236", "#cad900", "#73bfe6", "#d90057", "#ffa280", "#aaff00", "#397ee6", "#f27999", "#ff6600", "#a6d96c", "#4073ff", "#d9986c", "#50e639", "#3d00e6", "#e57a00", "#36d98d", "#b56cd9" };
    }
  }
}
