namespace ATLAS.i18n.Domain.Exceptions;

/// <summary>
/// Base exception for all ATLAS.i18n domain rule violations.
/// </summary>
public class I18nDomainException : Exception
{
    public I18nDomainException(string message)
        : base(message) { }

    public I18nDomainException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>Thrown when a requested language does not exist.</summary>
public class LanguageNotFoundException : I18nDomainException
{
    public string LanguageCode { get; }

    public LanguageNotFoundException(string languageCode)
        : base($"Language '{languageCode}' was not found.")
        => LanguageCode = languageCode;
}

/// <summary>Thrown when a requested translation does not exist.</summary>
public class TranslationNotFoundException : I18nDomainException
{
    public string TranslationCode { get; }
    public string LanguageCode    { get; }

    public TranslationNotFoundException(string code, string languageCode)
        : base($"Translation '{code}' for language '{languageCode}' was not found.")
    {
        TranslationCode = code;
        LanguageCode    = languageCode;
    }
}

/// <summary>Thrown when attempting to create a duplicate translation code+language pair.</summary>
public class DuplicateTranslationException : I18nDomainException
{
    public string TranslationCode { get; }
    public string LanguageCode    { get; }

    public DuplicateTranslationException(string code, string languageCode)
        : base($"A translation with code '{code}' already exists for language '{languageCode}'.")
    {
        TranslationCode = code;
        LanguageCode    = languageCode;
    }
}

/// <summary>Thrown when trying to deactivate the default language.</summary>
public class DefaultLanguageCannotBeDeactivatedException : I18nDomainException
{
    public DefaultLanguageCannotBeDeactivatedException(string languageCode)
        : base($"Language '{languageCode}' is the default language and cannot be deactivated.") { }
}

/// <summary>Thrown when locale configuration is not found for a language.</summary>
public class LocaleConfigurationNotFoundException : I18nDomainException
{
    public LocaleConfigurationNotFoundException(string languageCode)
        : base($"Locale configuration for language '{languageCode}' was not found.") { }
}
