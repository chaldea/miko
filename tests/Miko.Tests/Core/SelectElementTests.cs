using Miko.Core;
using Miko.Core.DomElements;
using Shouldly;

namespace Miko.Tests.Core;

public class SelectElementTests
{
    [Fact]
    public void TagName_ShouldReturnSelect()
    {
        var select = new SelectElement();
        select.TagName.ShouldBe("select");
    }

    [Fact]
    public void TagName_OptionElement_ShouldReturnOption()
    {
        var option = new OptionElement();
        option.TagName.ShouldBe("option");
    }

    [Fact]
    public void TagName_OptGroupElement_ShouldReturnOptgroup()
    {
        var optgroup = new OptGroupElement();
        optgroup.TagName.ShouldBe("optgroup");
    }

    [Fact]
    public void GetAllOptions_ShouldReturnAllDirectOptions()
    {
        var select = new SelectElement();
        var option1 = new OptionElement { TextContent = "Option 1" };
        var option2 = new OptionElement { TextContent = "Option 2" };

        select.AddChild(option1);
        select.AddChild(option2);

        var options = select.GetAllOptions();

        options.Count.ShouldBe(2);
        options[0].ShouldBe(option1);
        options[1].ShouldBe(option2);
    }

    [Fact]
    public void GetAllOptions_ShouldIncludeOptionsInOptGroup()
    {
        var select = new SelectElement();
        var option1 = new OptionElement { TextContent = "Option 1" };
        var optgroup = new OptGroupElement { Label = "Group 1" };
        var option2 = new OptionElement { TextContent = "Option 2" };
        var option3 = new OptionElement { TextContent = "Option 3" };

        optgroup.AddChild(option2);
        optgroup.AddChild(option3);
        select.AddChild(option1);
        select.AddChild(optgroup);

        var options = select.GetAllOptions();

        options.Count.ShouldBe(3);
        options[0].ShouldBe(option1);
        options[1].ShouldBe(option2);
        options[2].ShouldBe(option3);
    }

    [Fact]
    public void GetSelectedOption_ShouldReturnOptionBySelectedIndex()
    {
        var select = new SelectElement();
        var option1 = new OptionElement { TextContent = "Option 1" };
        var option2 = new OptionElement { TextContent = "Option 2" };

        select.AddChild(option1);
        select.AddChild(option2);
        select.SelectedIndex = 1;

        var selected = select.GetSelectedOption();

        selected.ShouldBe(option2);
    }

    [Fact]
    public void GetSelectedOption_ShouldReturnFirstSelectedOption_WhenNoIndex()
    {
        var select = new SelectElement();
        var option1 = new OptionElement { TextContent = "Option 1" };
        var option2 = new OptionElement { TextContent = "Option 2", Selected = true };

        select.AddChild(option1);
        select.AddChild(option2);

        var selected = select.GetSelectedOption();

        selected.ShouldBe(option2);
    }

    [Fact]
    public void GetSelectedOption_ShouldReturnNull_WhenNoSelection()
    {
        var select = new SelectElement();
        var option1 = new OptionElement { TextContent = "Option 1" };

        select.AddChild(option1);
        select.SelectedIndex = -1;

        var selected = select.GetSelectedOption();

        selected.ShouldBeNull();
    }

    [Fact]
    public void Value_Get_ShouldReturnSelectedOptionValue()
    {
        var select = new SelectElement();
        var option1 = new OptionElement { Value = "val1", TextContent = "Option 1" };
        var option2 = new OptionElement { Value = "val2", TextContent = "Option 2" };

        select.AddChild(option1);
        select.AddChild(option2);
        select.SelectedIndex = 1;

        select.Value.ShouldBe("val2");
    }

    [Fact]
    public void Value_Get_ShouldReturnTextContent_WhenValueIsNull()
    {
        var select = new SelectElement();
        var option1 = new OptionElement { TextContent = "Option 1" };

        select.AddChild(option1);
        select.SelectedIndex = 0;

        select.Value.ShouldBe("Option 1");
    }

    [Fact]
    public void Value_Set_ShouldSetSelectedIndex()
    {
        var select = new SelectElement();
        var option1 = new OptionElement { Value = "val1", TextContent = "Option 1" };
        var option2 = new OptionElement { Value = "val2", TextContent = "Option 2" };

        select.AddChild(option1);
        select.AddChild(option2);

        select.Value = "val2";

        select.SelectedIndex.ShouldBe(1);
        select.GetSelectedOption().ShouldBe(option2);
    }

    [Fact]
    public void Value_Set_ShouldSetSelectedIndexToMinusOne_WhenValueNotFound()
    {
        var select = new SelectElement();
        var option1 = new OptionElement { Value = "val1", TextContent = "Option 1" };

        select.AddChild(option1);
        select.SelectedIndex = 0;

        select.Value = "nonexistent";

        select.SelectedIndex.ShouldBe(-1);
    }

    [Fact]
    public void GetDisplayText_ShouldReturnSelectedOptionTextContent()
    {
        var select = new SelectElement();
        var option1 = new OptionElement { Value = "val1", TextContent = "Display Text" };

        select.AddChild(option1);
        select.SelectedIndex = 0;

        select.GetDisplayText().ShouldBe("Display Text");
    }

    [Fact]
    public void GetDisplayText_ShouldReturnEmptyString_WhenNoSelection()
    {
        var select = new SelectElement();
        var option1 = new OptionElement { TextContent = "Option 1" };

        select.AddChild(option1);
        select.SelectedIndex = -1;

        select.GetDisplayText().ShouldBe(string.Empty);
    }

    [Fact]
    public void Multiple_DefaultValue_ShouldBeFalse()
    {
        var select = new SelectElement();
        select.Multiple.ShouldBeFalse();
    }

    [Fact]
    public void Size_DefaultValue_ShouldBeOne()
    {
        var select = new SelectElement();
        select.Size.ShouldBe(1);
    }

    [Fact]
    public void IsOpen_DefaultValue_ShouldBeFalse()
    {
        var select = new SelectElement();
        select.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public void OptionElement_Value_ShouldBeSettable()
    {
        var option = new OptionElement();
        option.Value = "test-value";
        option.Value.ShouldBe("test-value");
    }

    [Fact]
    public void OptionElement_Selected_ShouldBeSettable()
    {
        var option = new OptionElement();
        option.Selected = true;
        option.Selected.ShouldBeTrue();
    }

    [Fact]
    public void OptGroupElement_Label_ShouldBeSettable()
    {
        var optgroup = new OptGroupElement();
        optgroup.Label = "My Group";
        optgroup.Label.ShouldBe("My Group");
    }

    [Fact]
    public void GetAllOptions_ShouldReturnEmptyList_WhenNoOptions()
    {
        var select = new SelectElement();

        var options = select.GetAllOptions();

        options.ShouldBeEmpty();
    }

    [Fact]
    public void NestedOptGroups_ShouldFlattenOptions()
    {
        var select = new SelectElement();
        var group1 = new OptGroupElement { Label = "Group 1" };
        var group2 = new OptGroupElement { Label = "Group 2" };
        var option1 = new OptionElement { TextContent = "A" };
        var option2 = new OptionElement { TextContent = "B" };
        var option3 = new OptionElement { TextContent = "C" };

        group1.AddChild(option1);
        group2.AddChild(option2);
        group2.AddChild(option3);
        select.AddChild(group1);
        select.AddChild(group2);

        var options = select.GetAllOptions();

        options.Count.ShouldBe(3);
        options[0].TextContent.ShouldBe("A");
        options[1].TextContent.ShouldBe("B");
        options[2].TextContent.ShouldBe("C");
    }

    #region IsOpen Behavior Tests

    [Fact]
    public void IsOpen_WhenChanged_ShouldMarkElementAsDirty()
    {
        var select = new SelectElement();
        select.IsDirty = false;

        select.IsOpen = true;

        select.IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void IsOpen_WhenSetToSameValue_ShouldNotMarkDirty()
    {
        var select = new SelectElement();
        select.IsOpen = false;
        select.IsDirty = false;

        select.IsOpen = false;

        select.IsDirty.ShouldBeFalse();
    }

    [Fact]
    public void Toggle_WhenClosed_ShouldOpen()
    {
        var select = new SelectElement();

        select.Toggle();

        select.IsOpen.ShouldBeTrue();
    }

    [Fact]
    public void Toggle_WhenOpen_ShouldClose()
    {
        var select = new SelectElement { IsOpen = true };

        select.Toggle();

        select.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public void Toggle_WhenDisabled_ShouldNotChange()
    {
        var select = new SelectElement();
        select.SetState(ElementState.Disabled);

        select.Toggle();

        select.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public void Close_ShouldSetIsOpenToFalse()
    {
        var select = new SelectElement { IsOpen = true };

        select.Close();

        select.IsOpen.ShouldBeFalse();
    }

    #endregion

    #region SelectOption Tests

    [Fact]
    public void SelectOption_WithValidIndex_ShouldUpdateSelectedIndex()
    {
        var select = new SelectElement();
        select.AddChild(new OptionElement { TextContent = "A" });
        select.AddChild(new OptionElement { TextContent = "B" });

        var result = select.SelectOption(1);

        result.ShouldBeTrue();
        select.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void SelectOption_ShouldCloseDropdown()
    {
        var select = new SelectElement { IsOpen = true };
        select.AddChild(new OptionElement { TextContent = "A" });

        select.SelectOption(0);

        select.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public void SelectOption_WithInvalidIndex_ShouldReturnFalse()
    {
        var select = new SelectElement();
        select.AddChild(new OptionElement { TextContent = "A" });

        var result = select.SelectOption(5);

        result.ShouldBeFalse();
    }

    [Fact]
    public void SelectOption_WithDisabledOption_ShouldReturnFalse()
    {
        var select = new SelectElement();
        var option = new OptionElement { TextContent = "A" };
        option.SetState(ElementState.Disabled);
        select.AddChild(option);

        var result = select.SelectOption(0);

        result.ShouldBeFalse();
    }

    [Fact]
    public void SelectOption_WhenValueUnchanged_ShouldReturnFalse()
    {
        var select = new SelectElement();
        select.AddChild(new OptionElement { TextContent = "A" });
        select.SelectedIndex = 0;

        var result = select.SelectOption(0);

        result.ShouldBeFalse();
    }

    #endregion

    #region SelectedIndex Dirty Marking Tests

    [Fact]
    public void SelectedIndex_WhenChanged_ShouldMarkDirty()
    {
        var select = new SelectElement();
        select.AddChild(new OptionElement { TextContent = "A" });
        select.IsDirty = false;

        select.SelectedIndex = 0;

        select.IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void SelectedIndex_WhenSetToSameValue_ShouldNotMarkDirty()
    {
        var select = new SelectElement();
        select.AddChild(new OptionElement { TextContent = "A" });
        select.SelectedIndex = 0;
        select.IsDirty = false;

        select.SelectedIndex = 0;

        select.IsDirty.ShouldBeFalse();
    }

    #endregion

    #region HandleBlur Tests

    [Fact]
    public void HandleBlur_ShouldCloseDropdown()
    {
        var select = new SelectElement { IsOpen = true };

        select.HandleBlur();

        select.IsOpen.ShouldBeFalse();
    }

    #endregion
}
