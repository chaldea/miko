using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;

namespace Miko.Examples.Bootstrap.Examples;

/// <summary>
/// Bootstrap-style form control demonstration.
/// </summary>
public static class FormControlExample
{
    public const string OutputFileName = "bootstrap-form-controls.png";
    public const string Title = "Bootstrap Form Control Examples";

    public static Element CreateDOM()
    {
        return new DivElement
        {
            Class = "container",
            Children =
            {
                new H1Element { TextContent = "Bootstrap Form Control Examples" },

                // Text Inputs Section
                new H2Element { TextContent = "Text Inputs" },
                CreateFormGroup("Email address", "email", InputType.Text, "name@example.com", "We'll never share your email."),
                CreateFormGroup("Password", "password", InputType.Password, "Password"),

                // Input Sizes Section
                new H2Element { TextContent = "Input Sizes" },
                new DivElement
                {
                    Class = "mb-3",
                    Children =
                    {
                        new LabelElement { TextContent = "Large Input", Class = "form-label" },
                        new InputElement
                        {
                            Type = InputType.Text,
                            Class = "form-control form-control-lg",
                            Placeholder = "Large input"
                        }
                    }
                },
                new DivElement
                {
                    Class = "mb-3",
                    Children =
                    {
                        new LabelElement { TextContent = "Default Input", Class = "form-label" },
                        new InputElement
                        {
                            Type = InputType.Text,
                            Class = "form-control",
                            Placeholder = "Default input"
                        }
                    }
                },
                new DivElement
                {
                    Class = "mb-3",
                    Children =
                    {
                        new LabelElement { TextContent = "Small Input", Class = "form-label" },
                        new InputElement
                        {
                            Type = InputType.Text,
                            Class = "form-control form-control-sm",
                            Placeholder = "Small input"
                        }
                    }
                },

                // Select Dropdown Section
                new H2Element { TextContent = "Select Dropdowns" },
                new DivElement
                {
                    Class = "mb-3",
                    Children =
                    {
                        new LabelElement { TextContent = "Select an option", Class = "form-label" },
                        new SelectElement
                        {
                            Class = "form-select",
                            SelectedIndex = 0,
                            Children =
                            {
                                new OptionElement { TextContent = "Open this select menu", Value = "" },
                                new OptionElement { TextContent = "One", Value = "1" },
                                new OptionElement { TextContent = "Two", Value = "2" },
                                new OptionElement { TextContent = "Three", Value = "3" }
                            }
                        }
                    }
                },
                new DivElement
                {
                    Class = "mb-3",
                    Children =
                    {
                        new LabelElement { TextContent = "Large Select", Class = "form-label" },
                        new SelectElement
                        {
                            Class = "form-select form-select-lg",
                            SelectedIndex = 0,
                            Children =
                            {
                                new OptionElement { TextContent = "Large select", Value = "" },
                                new OptionElement { TextContent = "Option A", Value = "a" },
                                new OptionElement { TextContent = "Option B", Value = "b" }
                            }
                        }
                    }
                },
                new DivElement
                {
                    Class = "mb-3",
                    Children =
                    {
                        new LabelElement { TextContent = "Small Select", Class = "form-label" },
                        new SelectElement
                        {
                            Class = "form-select form-select-sm",
                            SelectedIndex = 0,
                            Children =
                            {
                                new OptionElement { TextContent = "Small select", Value = "" },
                                new OptionElement { TextContent = "Option X", Value = "x" },
                                new OptionElement { TextContent = "Option Y", Value = "y" }
                            }
                        }
                    }
                },

                // Checkboxes Section
                new H2Element { TextContent = "Checkboxes" },
                new DivElement
                {
                    Class = "form-check",
                    Children =
                    {
                        new InputElement { Type = InputType.Checkbox, Id = "check1" },
                        new LabelElement { TextContent = "Default checkbox", Class = "form-check-label", For = "check1" }
                    }
                },
                new DivElement
                {
                    Class = "form-check",
                    Children =
                    {
                        new InputElement { Type = InputType.Checkbox, Id = "check2", Checked = true },
                        new LabelElement { TextContent = "Checked checkbox", Class = "form-check-label", For = "check2" }
                    }
                },

                // Radio Buttons Section
                new H2Element { TextContent = "Radio Buttons" },
                new DivElement
                {
                    Class = "form-check",
                    Children =
                    {
                        new InputElement { Type = InputType.Radio, Id = "radio1", Checked = true },
                        new LabelElement { TextContent = "Default radio", Class = "form-check-label", For = "radio1" }
                    }
                },
                new DivElement
                {
                    Class = "form-check",
                    Children =
                    {
                        new InputElement { Type = InputType.Radio, Id = "radio2" },
                        new LabelElement { TextContent = "Second radio", Class = "form-check-label", For = "radio2" }
                    }
                },
                new DivElement
                {
                    Class = "form-check",
                    Children =
                    {
                        new InputElement { Type = InputType.Radio, Id = "radio3" },
                        new LabelElement { TextContent = "Third radio", Class = "form-check-label", For = "radio3" }
                    }
                },

                // Range Input Section
                new H2Element { TextContent = "Range Input" },
                new DivElement
                {
                    Class = "mb-3",
                    Children =
                    {
                        new LabelElement { TextContent = "Example range", Class = "form-label" },
                        new InputElement
                        {
                            Type = InputType.Range,
                            Class = "form-range",
                            Min = 0,
                            Max = 100,
                            NumericValue = 50
                        }
                    }
                },

                // Validation States Section
                new H2Element { TextContent = "Validation States" },
                new DivElement
                {
                    Class = "mb-3",
                    Children =
                    {
                        new LabelElement { TextContent = "Valid Input", Class = "form-label" },
                        new InputElement
                        {
                            Type = InputType.Text,
                            Class = "form-control is-valid",
                            Value = "Correct value"
                        },
                        new DivElement { TextContent = "Looks good!", Class = "valid-feedback" }
                    }
                },
                new DivElement
                {
                    Class = "mb-3",
                    Children =
                    {
                        new LabelElement { TextContent = "Invalid Input", Class = "form-label" },
                        new InputElement
                        {
                            Type = InputType.Text,
                            Class = "form-control is-invalid",
                            Value = ""
                        },
                        new DivElement { TextContent = "Please provide a valid value.", Class = "invalid-feedback" }
                    }
                },

                // Input Group Section
                new H2Element { TextContent = "Input Groups" },
                new DivElement
                {
                    Class = "input-group mb-3",
                    Children =
                    {
                        new SpanElement { TextContent = "@", Class = "input-group-text" },
                        new InputElement
                        {
                            Type = InputType.Text,
                            Class = "form-control",
                            Placeholder = "Username"
                        }
                    }
                },
                new DivElement
                {
                    Class = "input-group mb-3",
                    Children =
                    {
                        new InputElement
                        {
                            Type = InputType.Text,
                            Class = "form-control",
                            Placeholder = "Amount"
                        },
                        new SpanElement { TextContent = ".00", Class = "input-group-text" }
                    }
                },
                new DivElement
                {
                    Class = "input-group mb-3",
                    Children =
                    {
                        new SpanElement { TextContent = "$", Class = "input-group-text" },
                        new InputElement
                        {
                            Type = InputType.Text,
                            Class = "form-control input-group-control",
                            Placeholder = "Amount"
                        },
                        new SpanElement { TextContent = ".00", Class = "input-group-text" }
                    }
                },
            }
        };
    }

    private static Element CreateFormGroup(string label, string id, InputType type, string placeholder, string? helpText = null)
    {
        var group = new DivElement
        {
            Class = "mb-3",
            Children =
            {
                new LabelElement { TextContent = label, Class = "form-label", For = id },
                new InputElement
                {
                    Type = type,
                    Class = "form-control",
                    Id = id,
                    Placeholder = placeholder
                }
            }
        };

        if (helpText != null)
        {
            group.AddChild(new DivElement { TextContent = helpText, Class = "form-text" });
        }

        return group;
    }
}
