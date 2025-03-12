namespace Nidikwa.FileEncoding;

public class NdkwFileFormatException : Exception
{
    public NdkwFileFormatException() : this(null)
    {
    }

    public NdkwFileFormatException(Exception? innerException) : base("Invalid Nidikwa file format.", innerException)
    {
    }
}
