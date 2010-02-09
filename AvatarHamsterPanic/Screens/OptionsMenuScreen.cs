#region File Description
//-----------------------------------------------------------------------------
// OptionsMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using System;
#endregion

namespace Menu
{
  /// <summary>
  /// The options screen is brought up over the top of the main menu
  /// screen, and gives the user a chance to configure the game
  /// in various hopefully useful ways.
  /// </summary>
  class OptionsMenuScreen : MenuScreen
  {
    #region Fields

    MenuEntry ungulateMenuEntry;
    MenuEntry languageMenuEntry;
    MenuEntry frobnicateMenuEntry;
    MenuEntry elfMenuEntry;

    enum Ungulate
    {
      BactrianCamel,
      Dromedary,
      Llama,
    }

    static Ungulate currentUngulate = Ungulate.Dromedary;

    static string[] languages = { "C#", "French", "Deoxyribonucleic acid" };
    static int currentLanguage = 0;

    static bool frobnicate = true;

    static int elf = 23;

    #endregion

    #region Initialization


    /// <summary>
    /// Constructor.
    /// </summary>
    public OptionsMenuScreen()
    {

    }

    public override void LoadContent()
    {
      // Create our menu entries.
      Vector2 entryPosition = new Vector2( 100f, 150f );

      // Select preferred ungulate
      ungulateMenuEntry = new MenuEntry( this, entryPosition, string.Empty );
      ungulateMenuEntry.Selected += UngulateMenuEntrySelected;
      MenuItems.Add( ungulateMenuEntry );
      entryPosition.Y += ungulateMenuEntry.Dimensions.Y;

      // Select preferred language
      languageMenuEntry = new MenuEntry( this, entryPosition, string.Empty );
      languageMenuEntry.Selected += LanguageMenuEntrySelected;
      MenuItems.Add( languageMenuEntry );
      entryPosition.Y += languageMenuEntry.Dimensions.Y;

      // Toggle frobnication
      frobnicateMenuEntry = new MenuEntry( this, entryPosition, string.Empty );
      frobnicateMenuEntry.Selected += FrobnicateMenuEntrySelected;
      MenuItems.Add( frobnicateMenuEntry );
      entryPosition.Y += frobnicateMenuEntry.Dimensions.Y;

      // Change the elf number
      elfMenuEntry = new MenuEntry( this, entryPosition, string.Empty );
      elfMenuEntry.Selected += ElfMenuEntrySelected;
      MenuItems.Add( elfMenuEntry );
      entryPosition.Y += elfMenuEntry.Dimensions.Y;

      SetMenuEntryText();

      MenuEntry backMenuEntry = new MenuEntry( this, entryPosition, "Back" );
      backMenuEntry.Selected += OnCancel;

      MenuEntries[0].Focused = true;
    }


    /// <summary>
    /// Fills in the latest values for the options screen menu text.
    /// </summary>
    void SetMenuEntryText()
    {
      ungulateMenuEntry.Text = "Preferred ungulate: " + currentUngulate;
      languageMenuEntry.Text = "Language: " + languages[currentLanguage];
      frobnicateMenuEntry.Text = "Frobnicate: " + ( frobnicate ? "on" : "off" );
      elfMenuEntry.Text = "elf: " + elf;
    }


    #endregion

    #region Handle Input


    /// <summary>
    /// Event handler for when the Ungulate menu entry is selected.
    /// </summary>
    void UngulateMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      currentUngulate++;

      if ( currentUngulate > Ungulate.Llama )
        currentUngulate = 0;

      SetMenuEntryText();
    }


    /// <summary>
    /// Event handler for when the Language menu entry is selected.
    /// </summary>
    void LanguageMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      currentLanguage = ( currentLanguage + 1 ) % languages.Length;

      SetMenuEntryText();
    }


    /// <summary>
    /// Event handler for when the Frobnicate menu entry is selected.
    /// </summary>
    void FrobnicateMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      frobnicate = !frobnicate;

      SetMenuEntryText();
    }


    /// <summary>
    /// Event handler for when the Elf menu entry is selected.
    /// </summary>
    void ElfMenuEntrySelected( object sender, PlayerIndexEventArgs e )
    {
      elf++;

      SetMenuEntryText();
    }


    #endregion
  }
}
