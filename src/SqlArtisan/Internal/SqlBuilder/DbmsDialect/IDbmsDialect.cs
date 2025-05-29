namespace SqlArtisan;

internal interface IDbmsDialect
{
    string GetParameterName(int index);
}
