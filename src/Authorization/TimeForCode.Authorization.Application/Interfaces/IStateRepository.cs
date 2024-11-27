using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IStateRepository
    {
        void AddState(StateEntry stateEntry);
        StateEntry? GetState(string state);
    }
}