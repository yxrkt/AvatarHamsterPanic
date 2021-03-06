#region Using Statements

using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

#endregion

namespace Debug
{
  [Flags]
  public enum Alignment
  {
    None = 0,
    Left = 1,
    Right = 2,
    HorizontalCenter = 4,
    Top = 8,
    Bottom = 16,
    VerticalCenter = 32,
    TopLeft = Top | Left,
    TopRight = Top | Right,
    TopCenter = Top | HorizontalCenter,
    BottomLeft = Bottom | Left,
    BottomRight = Bottom | Right,
    BottomCenter = Bottom | HorizontalCenter,
    CenterLeft = VerticalCenter | Left,
    CenterRight = VerticalCenter | Right,
    Center = VerticalCenter | HorizontalCenter
  }

  public struct Layout
  {
    #region Fields

    public Rectangle ClientArea;
    public Rectangle SafeArea;

    #endregion

    #region Initialization

    public Layout( Rectangle clientArea, Rectangle safeArea )
    {
      ClientArea = clientArea;
      SafeArea = safeArea;
    }

    public Layout( Rectangle clientArea )
      : this( clientArea, clientArea )
    {
    }

    public Layout( Viewport viewport )
    {
      ClientArea = new Rectangle( (int)viewport.X, (int)viewport.Y,
                                  (int)viewport.Width, (int)viewport.Height );
      SafeArea = viewport.TitleSafeArea;
    }

    #endregion

    public Vector2 Place( Vector2 size, float horizontalMargin,
                                        float verticalMargine, Alignment alignment )
    {
      Rectangle rc = new Rectangle( 0, 0, (int)size.X, (int)size.Y );
      rc = Place( rc, horizontalMargin, verticalMargine, alignment );
      return new Vector2( rc.X, rc.Y );
    }

    public Rectangle Place( Rectangle region, float horizontalMargin,
                                        float verticalMargine, Alignment alignment )
    {
      if ( ( alignment & Alignment.Left ) != 0 )
      {
        region.X = ClientArea.X + (int)( ClientArea.Width * horizontalMargin );
      }
      else if ( ( alignment & Alignment.Right ) != 0 )
      {
        region.X = ClientArea.X +
                    (int)( ClientArea.Width * ( 1.0f - horizontalMargin ) ) -
                    region.Width;
      }
      else if ( ( alignment & Alignment.HorizontalCenter ) != 0 )
      {
        region.X = ClientArea.X + ( ClientArea.Width - region.Width ) / 2 +
                    (int)( horizontalMargin * ClientArea.Width );
      }
      else
      {
      }

      if ( ( alignment & Alignment.Top ) != 0 )
      {
        region.Y = ClientArea.Y + (int)( ClientArea.Height * verticalMargine );
      }
      else if ( ( alignment & Alignment.Bottom ) != 0 )
      {
        region.Y = ClientArea.Y +
                    (int)( ClientArea.Height * ( 1.0f - verticalMargine ) ) -
                    region.Height;
      }
      else if ( ( alignment & Alignment.VerticalCenter ) != 0 )
      {
        region.Y = ClientArea.Y + ( ClientArea.Height - region.Height ) / 2 +
                    (int)( verticalMargine * ClientArea.Height );
      }
      else
      {
      }

      if ( region.Left < SafeArea.Left )
        region.X = SafeArea.Left;

      if ( region.Right > SafeArea.Right )
        region.X = SafeArea.Right - region.Width;

      if ( region.Top < SafeArea.Top )
        region.Y = SafeArea.Top;

      if ( region.Bottom > SafeArea.Bottom )
        region.Y = SafeArea.Bottom - region.Height;

      return region;
    }
  }
}