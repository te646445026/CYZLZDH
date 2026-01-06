using CYZLZDH.Core.Models;

namespace CYZLZDH.Core.Interfaces;

public interface IKeyService
{
    KEY CheckKey();
    string GetProviderType();
}
