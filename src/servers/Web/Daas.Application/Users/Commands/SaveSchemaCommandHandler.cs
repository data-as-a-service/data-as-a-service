//using MediatR;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using Daas.Application.DTO.ResponseDTO;
//using Daas.Application.Users.Queries;

//namespace Daas.Application.Users.Commands
//{
//    public class SaveSchemaCommandHandler : IRequestHandler<SaveSchemaCommand, SaveSchemaResponseDTO>
//    {
//        public Task<SaveSchemaResponseDTO> Handle(SaveSchemaCommand request, CancellationToken cancellationToken)
//        {
//            Guid newuserid;
//            int i=0;
//            string FieldingName="";
//            FieldType FieldingType ;
//            int rows = request.sus.Length;
//            while (rows > 0) { 
//                newuserid = Guid.NewGuid();
//                FieldingName=request.sus[i].fieldName;
//                FieldingType = request.sus[i].fieldType;
//                rows--;
//            }
//            return new SaveSchemaResponseDTO();
//        }
//    }
//}
