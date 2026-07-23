namespace Foldfinch.Core.Pdf;

/// <summary>
/// A PDF load/save failure with a message already phrased for the user (e.g. "it's password-protected").
/// The UI shows <see cref="System.Exception.Message"/> directly.
/// </summary>
public sealed class PdfOperationException(string message, Exception? inner = null)
    : Exception(message, inner);
