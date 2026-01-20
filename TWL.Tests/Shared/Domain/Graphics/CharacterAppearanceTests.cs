using System.Collections.Generic;
using Xunit;
using TWL.Shared.Domain.Graphics;
using TWL.Shared.Domain.Requests;

namespace TWL.Tests.Shared.Domain.Graphics
{
    public class CharacterAppearanceTests
    {
        [Fact]
        public void CharacterAppearance_Defaults_AreSet()
        {
            var appearance = new CharacterAppearance();
            Assert.Equal("default_body", appearance.BaseBodyId);
            Assert.Equal(PaletteType.Normal, appearance.Palette);
            Assert.Empty(appearance.EquipmentVisuals);
        }

        [Fact]
        public void AvatarPart_Properties_AreSet()
        {
            var part = new AvatarPart(EquipmentSlot.Head, "helm_01", PaletteType.Fire);
            Assert.Equal(EquipmentSlot.Head, part.Slot);
            Assert.Equal("helm_01", part.AssetId);
            Assert.Equal(PaletteType.Fire, part.PaletteOverride);
        }

        [Fact]
        public void CharacterAppearance_CanAddParts()
        {
            var appearance = new CharacterAppearance();
            var part = new AvatarPart(EquipmentSlot.Weapon, "sword_01");
            appearance.EquipmentVisuals.Add(part);

            Assert.Single(appearance.EquipmentVisuals);
            Assert.Equal("sword_01", appearance.EquipmentVisuals[0].AssetId);
        }
    }
}
