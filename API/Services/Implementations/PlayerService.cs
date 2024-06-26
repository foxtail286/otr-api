using API.DTOs;
using API.Entities;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using AutoMapper;

namespace API.Services.Implementations;

public class PlayerService(IPlayerRepository playerRepository, IMapper mapper) : IPlayerService
{
    private readonly IMapper _mapper = mapper;
    private readonly IPlayerRepository _playerRepository = playerRepository;

    public async Task<IEnumerable<PlayerDTO>> GetAllAsync() =>
        _mapper.Map<IEnumerable<PlayerDTO>>(await _playerRepository.GetAsync());

    public async Task<IEnumerable<PlayerRanksDTO>> GetAllRanksAsync() =>
        _mapper.Map<IEnumerable<PlayerRanksDTO>>(await _playerRepository.GetAllAsync());

    public async Task<int?> GetIdAsync(int userId)
    {
        return await _playerRepository.GetIdAsync(userId);
    }

    public async Task<IEnumerable<PlayerIdMappingDTO>> GetIdMappingAsync() =>
        await _playerRepository.GetIdMappingAsync();

    public async Task<IEnumerable<PlayerCountryMappingDTO>> GetCountryMappingAsync() =>
        await _playerRepository.GetCountryMappingAsync();

    public async Task<PlayerInfoDTO?> GetVersatileAsync(string key) =>
        _mapper.Map<PlayerInfoDTO?>(await _playerRepository.GetVersatileAsync(key, false));

    public async Task<IEnumerable<PlayerInfoDTO>> GetAsync(IEnumerable<long> osuIds)
    {
        var idList = osuIds.ToList();
        var players = (await _playerRepository.GetAsync(idList)).ToList();
        var dtos = new List<PlayerInfoDTO>();

        // Iterate through the players, on null items create a default DTO but store the osuId.
        // This tells the caller that we don't have info on a specific player.

        for (var i = 0; i < players.Count; i++)
        {
            Player? curPlayer = players[i];
            if (curPlayer is not null)
            {
                dtos.Add(_mapper.Map<PlayerInfoDTO>(curPlayer));
            }
            else
            {
                dtos.Add(new PlayerInfoDTO
                {
                    OsuId = idList.ElementAt(i)
                });
            }
        }

        return dtos;
    }
}
