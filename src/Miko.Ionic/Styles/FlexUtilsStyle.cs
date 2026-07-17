// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Styles
{
    /// <summary>
    /// Flexbox utility classes for controlling layout, alignment, and sizing.
    /// Provides utilities for flex direction, alignment, justification, wrapping, and ordering.
    /// </summary>
    public static class FlexUtilsStyle
    {
        public static CssObject GenStyle()
        {
            var styles = new CssObject();

            // Align Content
            styles[".ion-align-content-start"] = new() { AlignContent = AlignContent.FlexStart };
            styles[".ion-align-content-end"] = new() { AlignContent = AlignContent.FlexEnd };
            styles[".ion-align-content-center"] = new() { AlignContent = AlignContent.Center };
            styles[".ion-align-content-between"] = new() { AlignContent = AlignContent.SpaceBetween };
            styles[".ion-align-content-around"] = new() { AlignContent = AlignContent.SpaceAround };
            styles[".ion-align-content-stretch"] = new() { AlignContent = AlignContent.Stretch };

            // Align Items
            styles[".ion-align-items-start"] = new() { AlignItems = AlignItems.FlexStart };
            styles[".ion-align-items-end"] = new() { AlignItems = AlignItems.FlexEnd };
            styles[".ion-align-items-center"] = new() { AlignItems = AlignItems.Center };
            styles[".ion-align-items-stretch"] = new() { AlignItems = AlignItems.Stretch };
            styles[".ion-align-items-baseline"] = new() { AlignItems = AlignItems.Baseline };

            // Align Self
            styles[".ion-align-self-start"] = new() { AlignSelf = AlignSelf.FlexStart };
            styles[".ion-align-self-end"] = new() { AlignSelf = AlignSelf.FlexEnd };
            styles[".ion-align-self-center"] = new() { AlignSelf = AlignSelf.Center };
            styles[".ion-align-self-stretch"] = new() { AlignSelf = AlignSelf.Stretch };
            styles[".ion-align-self-baseline"] = new() { AlignSelf = AlignSelf.Baseline };
            styles[".ion-align-self-auto"] = new() { AlignSelf = AlignSelf.Auto };

            // Justify Content
            styles[".ion-justify-content-start"] = new() { JustifyContent = JustifyContent.FlexStart };
            styles[".ion-justify-content-end"] = new() { JustifyContent = JustifyContent.FlexEnd };
            styles[".ion-justify-content-center"] = new() { JustifyContent = JustifyContent.Center };
            styles[".ion-justify-content-between"] = new() { JustifyContent = JustifyContent.SpaceBetween };
            styles[".ion-justify-content-around"] = new() { JustifyContent = JustifyContent.SpaceAround };
            styles[".ion-justify-content-evenly"] = new() { JustifyContent = JustifyContent.SpaceEvenly };

            // Flex Direction
            styles[".ion-flex-row"] = new() { FlexDirection = FlexDirection.Row };
            styles[".ion-flex-row-reverse"] = new() { FlexDirection = FlexDirection.RowReverse };
            styles[".ion-flex-column"] = new() { FlexDirection = FlexDirection.Column };
            styles[".ion-flex-column-reverse"] = new() { FlexDirection = FlexDirection.ColumnReverse };

            // Flex Wrap
            styles[".ion-wrap"] = new() { FlexWrap = FlexWrap.Wrap };
            styles[".ion-nowrap"] = new() { FlexWrap = FlexWrap.Nowrap };
            styles[".ion-wrap-reverse"] = new() { FlexWrap = FlexWrap.WrapReverse };
            styles[".ion-flex-wrap"] = new() { FlexWrap = FlexWrap.Wrap };
            styles[".ion-flex-nowrap"] = new() { FlexWrap = FlexWrap.Nowrap };
            styles[".ion-flex-wrap-reverse"] = new() { FlexWrap = FlexWrap.WrapReverse };

            // Flex Fill
            styles[".ion-flex-1"] = new() { Flex = 1 };
            // Note: auto, initial, none need to be set via FlexGrow/FlexShrink/FlexBasis
            styles[".ion-flex-auto"] = new() { FlexGrow = 1, FlexShrink = 1, FlexBasis = Length.Auto };
            styles[".ion-flex-initial"] = new() { FlexGrow = 0, FlexShrink = 1, FlexBasis = Length.Auto };
            styles[".ion-flex-none"] = new() { FlexGrow = 0, FlexShrink = 0, FlexBasis = Length.Auto };

            // Flex Grow and Shrink
            styles[".ion-flex-grow-0"] = new() { FlexGrow = 0 };
            styles[".ion-flex-grow-1"] = new() { FlexGrow = 1 };
            styles[".ion-flex-shrink-0"] = new() { FlexShrink = 0 };
            styles[".ion-flex-shrink-1"] = new() { FlexShrink = 1 };

            // Flex Order
            styles[".ion-order-first"] = new() { Order = -1 };
            for (int i = 0; i <= 12; i++)
            {
                styles[$".ion-order-{i}"] = new() { Order = i };
            }
            styles[".ion-order-last"] = new() { Order = 13 };

            return styles;
        }
    }
}
