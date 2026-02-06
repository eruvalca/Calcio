using Calcio.Shared.Validation;

using Shouldly;

namespace Calcio.UnitTests.Validation;

public class PlayerImportColumnMappingTests
{
    #region FindMatchingField Tests - Required Fields

    [Theory]
    [InlineData("FirstName")]
    [InlineData("firstname")]
    [InlineData("FIRSTNAME")]
    [InlineData("First Name")]
    [InlineData("first name")]
    [InlineData("First")]
    [InlineData("first")]
    [InlineData("GivenName")]
    [InlineData("given_name")]
    public void FindMatchingField_FirstNameAliases_ReturnsFirstName(string columnName)
    {
        // Act
        var result = PlayerImportColumnMapping.FindMatchingField(columnName);

        // Assert
        result.ShouldBe("FirstName");
    }

    [Theory]
    [InlineData("LastName")]
    [InlineData("lastname")]
    [InlineData("LASTNAME")]
    [InlineData("Last Name")]
    [InlineData("last name")]
    [InlineData("Last")]
    [InlineData("Surname")]
    [InlineData("FamilyName")]
    [InlineData("family_name")]
    public void FindMatchingField_LastNameAliases_ReturnsLastName(string columnName)
    {
        // Act
        var result = PlayerImportColumnMapping.FindMatchingField(columnName);

        // Assert
        result.ShouldBe("LastName");
    }

    [Theory]
    [InlineData("DateOfBirth")]
    [InlineData("dateofbirth")]
    [InlineData("DOB")]
    [InlineData("dob")]
    [InlineData("Date Of Birth")]
    [InlineData("date of birth")]
    [InlineData("Birth Date")]
    [InlineData("BirthDate")]
    [InlineData("Birthday")]
    public void FindMatchingField_DateOfBirthAliases_ReturnsDateOfBirth(string columnName)
    {
        // Act
        var result = PlayerImportColumnMapping.FindMatchingField(columnName);

        // Assert
        result.ShouldBe("DateOfBirth");
    }

    [Theory]
    [InlineData("Gender")]
    [InlineData("gender")]
    [InlineData("GENDER")]
    [InlineData("Sex")]
    [InlineData("sex")]
    public void FindMatchingField_GenderAliases_ReturnsGender(string columnName)
    {
        // Act
        var result = PlayerImportColumnMapping.FindMatchingField(columnName);

        // Assert
        result.ShouldBe("Gender");
    }

    #endregion

    #region FindMatchingField Tests - Optional Fields

    [Theory]
    [InlineData("GraduationYear")]
    [InlineData("graduationyear")]
    [InlineData("Graduation Year")]
    [InlineData("graduation year")]
    [InlineData("Grad Year")]
    [InlineData("GradYear")]
    [InlineData("grad_year")]
    [InlineData("class_of")]
    [InlineData("ClassOf")]
    public void FindMatchingField_GraduationYearAliases_ReturnsGraduationYear(string columnName)
    {
        // Act
        var result = PlayerImportColumnMapping.FindMatchingField(columnName);

        // Assert
        result.ShouldBe("GraduationYear");
    }

    [Theory]
    [InlineData("JerseyNumber")]
    [InlineData("jerseynumber")]
    [InlineData("Jersey Number")]
    [InlineData("Jersey")]
    [InlineData("jersey")]
    [InlineData("Jersey #")]
    [InlineData("Jersey#")]
    [InlineData("Number")]
    [InlineData("player_number")]
    public void FindMatchingField_JerseyNumberAliases_ReturnsJerseyNumber(string columnName)
    {
        // Act
        var result = PlayerImportColumnMapping.FindMatchingField(columnName);

        // Assert
        result.ShouldBe("JerseyNumber");
    }

    [Theory]
    [InlineData("TryoutNumber")]
    [InlineData("tryoutnumber")]
    [InlineData("Tryout Number")]
    [InlineData("tryout number")]
    [InlineData("Tryout")]
    [InlineData("Tryout #")]
    [InlineData("Tryout#")]
    public void FindMatchingField_TryoutNumberAliases_ReturnsTryoutNumber(string columnName)
    {
        // Act
        var result = PlayerImportColumnMapping.FindMatchingField(columnName);

        // Assert
        result.ShouldBe("TryoutNumber");
    }

    #endregion

    #region FindMatchingField Tests - Unrecognized

    [Theory]
    [InlineData("UnknownColumn")]
    [InlineData("RandomText")]
    [InlineData("Player Name")]
    [InlineData("Age")]
    [InlineData("Position")]
    [InlineData("Team")]
    [InlineData("")]
    public void FindMatchingField_UnrecognizedColumn_ReturnsNull(string columnName)
    {
        // Act
        var result = PlayerImportColumnMapping.FindMatchingField(columnName);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region RequiredFields Tests

    [Fact]
    public void RequiredFields_ContainsAllRequiredFields()
    {
        // Assert
        PlayerImportColumnMapping.RequiredFields.ShouldContainKey("FirstName");
        PlayerImportColumnMapping.RequiredFields.ShouldContainKey("LastName");
        PlayerImportColumnMapping.RequiredFields.ShouldContainKey("DateOfBirth");
        PlayerImportColumnMapping.RequiredFields.ShouldContainKey("Gender");
        PlayerImportColumnMapping.RequiredFields.Count.ShouldBe(4);
    }

    #endregion

    #region OptionalFields Tests

    [Fact]
    public void OptionalFields_ContainsAllOptionalFields()
    {
        // Assert
        PlayerImportColumnMapping.OptionalFields.ShouldContainKey("GraduationYear");
        PlayerImportColumnMapping.OptionalFields.ShouldContainKey("JerseyNumber");
        PlayerImportColumnMapping.OptionalFields.ShouldContainKey("TryoutNumber");
        PlayerImportColumnMapping.OptionalFields.Count.ShouldBe(3);
    }

    #endregion

    #region Template Headers Tests

    [Fact]
    public void TemplateHeaders_ContainsAllRequiredAndOptionalFields()
    {
        // Assert - these are snake_case for CSV compatibility
        PlayerImportColumnMapping.TemplateHeaders.ShouldContain("first_name");
        PlayerImportColumnMapping.TemplateHeaders.ShouldContain("last_name");
        PlayerImportColumnMapping.TemplateHeaders.ShouldContain("date_of_birth");
        PlayerImportColumnMapping.TemplateHeaders.ShouldContain("gender");
        PlayerImportColumnMapping.TemplateHeaders.ShouldContain("graduation_year");
        PlayerImportColumnMapping.TemplateHeaders.ShouldContain("jersey_number");
        PlayerImportColumnMapping.TemplateHeaders.ShouldContain("tryout_number");
        PlayerImportColumnMapping.TemplateHeaders.Count.ShouldBe(7);
    }

    [Fact]
    public void TemplateDisplayHeaders_ContainsHumanReadableHeaders()
    {
        // Assert
        PlayerImportColumnMapping.TemplateDisplayHeaders.ShouldContain("First Name");
        PlayerImportColumnMapping.TemplateDisplayHeaders.ShouldContain("Last Name");
        PlayerImportColumnMapping.TemplateDisplayHeaders.ShouldContain("Date of Birth");
        PlayerImportColumnMapping.TemplateDisplayHeaders.ShouldContain("Gender");
        PlayerImportColumnMapping.TemplateDisplayHeaders.ShouldContain("Graduation Year");
        PlayerImportColumnMapping.TemplateDisplayHeaders.ShouldContain("Jersey Number");
        PlayerImportColumnMapping.TemplateDisplayHeaders.ShouldContain("Tryout Number");
        PlayerImportColumnMapping.TemplateDisplayHeaders.Count.ShouldBe(7);
    }

    #endregion

    #region Case Insensitivity Tests

    [Theory]
    [InlineData("FIRSTNAME", "FirstName")]
    [InlineData("firstname", "FirstName")]
    [InlineData("FirstName", "FirstName")]
    [InlineData("fIrStNaMe", "FirstName")]
    public void FindMatchingField_IsCaseInsensitive(string input, string expected)
    {
        // Act
        var result = PlayerImportColumnMapping.FindMatchingField(input);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion
}
