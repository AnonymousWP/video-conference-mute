using Vcm.Core;

namespace Vcm.Core.Tests;

public class ModifierTrackerTests
{
    [Fact]
    public void CapsLock_IsNotAModifier()
    {
        var t = new ModifierTracker();
        t.Update(0x14, true);
        t.Update(0x14, false);
        Assert.False(t.Win || t.Ctrl || t.Alt || t.Shift);
    }

    [Fact]
    public void WinHeldAcrossRepeatedKeys_StaysDown()
    {
        var t = new ModifierTracker();
        t.Update(KeyNames.VK_LWIN, true);
        for (int i = 0; i < 3; i++)
        {
            t.Update('A', true);
            t.Update('A', false);
        }
        Assert.True(t.Win);
        Assert.False(t.Shift);
    }

    [Fact]
    public void LeftAndRightVariants_AreTrackedSeparately()
    {
        var t = new ModifierTracker();
        t.Update(KeyNames.VK_LSHIFT, true);
        t.Update(KeyNames.VK_RSHIFT, true);
        t.Update(KeyNames.VK_LSHIFT, false);
        Assert.True(t.Shift);
        t.Update(KeyNames.VK_RSHIFT, false);
        Assert.False(t.Shift);
    }

    [Fact]
    public void GenericCodes_MapToTheLeftKey()
    {
        var t = new ModifierTracker();
        t.Update(KeyNames.VK_CONTROL, true);
        t.Update(KeyNames.VK_MENU, true);
        Assert.True(t.Ctrl);
        Assert.True(t.Alt);
        t.Update(KeyNames.VK_LCONTROL, false);
        t.Update(KeyNames.VK_LMENU, false);
        Assert.False(t.Ctrl);
        Assert.False(t.Alt);
    }

    [Fact]
    public void Reset_TakesTheGivenState()
    {
        var t = new ModifierTracker();
        t.Update(KeyNames.VK_LWIN, true);
        t.Reset(vk => vk == KeyNames.VK_RCONTROL);
        Assert.False(t.Win);
        Assert.True(t.Ctrl);
    }
}
