namespace SG.LoyaltyApps.API.Common
{
    public interface IEnService
    {
        string ecr(string plainText);
        string dcr(string cipherText);
    }
}
