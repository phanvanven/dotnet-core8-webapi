using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Extensions;
using api.interfaces;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/portfolio")]
    [ApiController]
    public class PortfolioController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly IStockRepository _stockRepository;
        private readonly IPortfolioRepository _portfolioRepository;
        private readonly IFMPService _fmpService;
        public PortfolioController
        (
            UserManager<User> userManager,
            IStockRepository stockRepository,
            IPortfolioRepository portfolioRepository,
            IFMPService fMPService
        )
        {
            _userManager = userManager;
            _stockRepository = stockRepository;
            _portfolioRepository = portfolioRepository;
            _fmpService = fMPService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserPortfolio()
        {
            var username = User.GetUsername();
            var user = await _userManager.FindByNameAsync(username);
            var userPortfolio = await _portfolioRepository.GetUserPortfolio(user);
            return Ok(userPortfolio);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddPortfolio(string symbol)
        {
            var username = User.GetUsername();
            var user = await _userManager.FindByNameAsync(username);
            var stock = await _stockRepository.GetBySymbolAsync(symbol);
            if(stock == null){
                stock  = await _fmpService.FindStockBySymbolAsync(symbol);
                if(stock == null){
                    return BadRequest("Stock does not exists");
                }else {
                    await _stockRepository.CreateAsync(stock);
                }
            }
            // if (stock == null)
            // {
            //     return BadRequest("Stock not found");
            // }

            var userPortfolio = await _portfolioRepository.GetUserPortfolio(user);
            if (userPortfolio.Any(e => e.Symbol.ToLower() == symbol.ToLower()))
            {
                return BadRequest("Cannot add same stock to portfolio");
            }

            var portfolioModel = new Portfolio
            {
                StockId = stock.Id,
                UserId = user.Id,
            };

            await _portfolioRepository.CreateAsync(portfolioModel);
            if (portfolioModel == null)
            {
                return StatusCode(500, "Could not create");
            }

            return Created();
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeletePortfolio(string symbol)
        {
            var username = User.GetUsername();
            var user = await _userManager.FindByNameAsync(username);
            var userPortfolio = await _portfolioRepository.GetUserPortfolio(user);
            var filterdStock = userPortfolio.Where(e => e.Symbol.ToLower() == symbol.ToLower()).ToList();
            if(filterdStock.Count() == 1){
                await _portfolioRepository.DeletePortfolioAsync(user, symbol);
                return Ok();
            }

            return BadRequest("Stock not in your portfolio");
        }
    }


}