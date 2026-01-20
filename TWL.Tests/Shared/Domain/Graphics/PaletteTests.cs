using System.Collections.Generic;
using Xunit;
using TWL.Shared.Domain.Graphics;

namespace TWL.Tests.Shared.Domain.Graphics
{
    public class PaletteTests
    {
        [Fact]
        public void PaletteRegistry_ReturnsDefault_WhenVariantNotFound()
        {
            var variant = PaletteRegistry.Get(PaletteType.Normal);
            Assert.NotNull(variant);
            Assert.Equal("Default", variant.Name);
        }

        [Fact]
        public void PaletteRegistry_RegistersAndRetrievesVariant()
        {
            var fire = PaletteRegistry.Get(PaletteType.Fire);
            Assert.NotNull(fire);
            Assert.Equal("Fire", fire.Name);
            Assert.True(fire.ColorReplacements.ContainsKey("#0000FF"));
            Assert.Equal("#FF0000", fire.ColorReplacements["#0000FF"]);
        }
    }
}
