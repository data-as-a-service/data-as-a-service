using MediatR;
using Daas.Domain.Entities;
using Daas.Application.DTO.RequestDTO;
using System.Collections;
namespace Daas.Application.Users.Queries;


public record GetPayloadQuery( RequestModel[]  sus,int howmany ):IRequest<ArrayList>;
 
