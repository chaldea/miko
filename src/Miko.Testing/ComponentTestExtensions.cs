namespace Miko.Testing;

/// <summary>
/// Extension methods for common component testing assertions.
/// </summary>
public static class ComponentTestExtensions
{
    /// <summary>
    /// Asserts that the component contains an element with the specified ID.
    /// </summary>
    public static ComponentUnderTest ShouldContainId(this ComponentUnderTest cut, string id)
    {
        var element = cut.FindById(id);
        if (element == null)
        {
            throw new AssertionException($"Expected to find element with ID '{id}', but it was not found.");
        }
        return cut;
    }

    /// <summary>
    /// Asserts that the component contains at least one element with the specified class.
    /// </summary>
    public static ComponentUnderTest ShouldContainClass(this ComponentUnderTest cut, string className)
    {
        var elements = cut.FindByClass(className);
        if (elements.Count == 0)
        {
            throw new AssertionException($"Expected to find at least one element with class '{className}', but none were found.");
        }
        return cut;
    }

    /// <summary>
    /// Asserts that the component contains at least one element with the specified tag name.
    /// </summary>
    public static ComponentUnderTest ShouldContainTag(this ComponentUnderTest cut, string tagName)
    {
        var elements = cut.FindByTagName(tagName);
        if (elements.Count == 0)
        {
            throw new AssertionException($"Expected to find at least one element with tag '{tagName}', but none were found.");
        }
        return cut;
    }

    /// <summary>
    /// Asserts that the component's text content contains the specified text.
    /// </summary>
    public static ComponentUnderTest ShouldContainText(this ComponentUnderTest cut, string expectedText)
    {
        var actualText = cut.GetTextContent();
        if (!actualText.Contains(expectedText))
        {
            throw new AssertionException($"Expected component text to contain '{expectedText}', but actual text was '{actualText}'.");
        }
        return cut;
    }
}

/// <summary>
/// Exception thrown when a component test assertion fails.
/// </summary>
public class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}
