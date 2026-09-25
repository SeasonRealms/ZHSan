using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameGlobal;
using GameObjects;
using Microsoft.Xna.Framework;
using WorldOfTheThreeKingdoms;
using Microsoft.Xna.Framework.Graphics;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameScreens.ScreenLayers

{
    public class MapVeilLayer
    {
        // B0：原為 new Color(new Vector4(...))，SeasonXNA 無 Vector4 類型，改用等價的 4 參 float 構造。
        // SeasonXNA：D01-A 繪製契約要求 premultiplied tint（RGB ≤ Alpha），原版 straight 色需顯式預乘換算，
        // 否則繪製時拋 NotSupportedException；FromNonPremultiplied(204,204,204,α) 即 straight(0.8,0.8,0.8,α) 的預乘表示。
        private Color BlendColorFull = Color.FromNonPremultiplied(204, 204, 204, 0);      // straight (0.8,0.8,0.8,0f)
        private Color BlendColorHigh = Color.FromNonPremultiplied(204, 204, 204, 22);     // straight (0.8,0.8,0.8,0.09f)
        private Color BlendColorLow = Color.FromNonPremultiplied(204, 204, 204, 68);      // straight (0.8,0.8,0.8,0.27f)
        private Color BlendColorMiddle = Color.FromNonPremultiplied(204, 204, 204, 45);   // straight (0.8,0.8,0.8,0.18f)
        private Color BlendColorNone = Color.FromNonPremultiplied(204, 204, 204, 153);    // straight (0.8,0.8,0.8,0.6f)

        private PlatformTexture veilTexture;

        public void Draw(Point viewportSize)
        {
            if ((Setting.Current.GlobalVariables.DrawMapVeil && !Session.GlobalVariables.SkyEye) && !Session.Current.Scenario.NoCurrentPlayer)
            {
                foreach (Tile tile in Session.MainGame.mainGameScreen.mainMapLayer.DisplayingTiles)
                {
                    Rectangle? nullable;
                    switch (this.CurrentPlayer.GetKnownAreaDataNoCheck(tile.Position))
                    {
                        case InformationLevel.无:
                            nullable = null;
                            CacheManager.Draw(this.veilTexture, tile.Destination, nullable, this.BlendColorNone, 0f, Vector2.Zero, SpriteEffects.None, 0.6f);
                            break;

                        case InformationLevel.低:
                            nullable = null;
                            CacheManager.Draw(this.veilTexture, tile.Destination, nullable, this.BlendColorLow, 0f, Vector2.Zero, SpriteEffects.None, 0.6f);
                            break;

                        case InformationLevel.中:
                            nullable = null;
                            CacheManager.Draw(this.veilTexture, tile.Destination, nullable, this.BlendColorMiddle, 0f, Vector2.Zero, SpriteEffects.None, 0.6f);
                            break;

                        case InformationLevel.高:
                            nullable = null;
                            CacheManager.Draw(this.veilTexture, tile.Destination, nullable, this.BlendColorHigh, 0f, Vector2.Zero, SpriteEffects.None, 0.6f);
                            break;
                    }
                }
            }
        }

        public void Initialize(MainGameScreen screen)
        {
            this.veilTexture = screen.Textures.MapVeilTextures[0];
        }

        private Faction CurrentPlayer
        {
            get
            {
                return Session.Current.Scenario.CurrentPlayer;
            }
        }
    }


}
