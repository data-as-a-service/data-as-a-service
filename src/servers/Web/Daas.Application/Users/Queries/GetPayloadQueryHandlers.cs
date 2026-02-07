using Daas.Domain.Entities;
using MediatR;
using MediatR.NotificationPublishers;
using Microsoft.VisualBasic.FileIO;
using System.Collections;
using System.Dynamic;
using System.Security.Cryptography.X509Certificates;

namespace Daas.Application.Users.Queries;

public class GetPayloadQueryHandlers : IRequestHandler<GetPayloadQuery, ArrayList>
{
    public Task<ArrayList> Handle(GetPayloadQuery request, CancellationToken cancellationToken)
    {
        Dictionary<string, string> x = new Dictionary<string, string>();

        //ResultModel data = new ResultModel();
        ArrayList data = new ArrayList();
        int iterations = request.howmany;
        string s;
        string answer;
        while (iterations>0)
        {
            var row = new ExpandoObject() as IDictionary<string, object>;
            for (int i = 0; i < request.sus.Length; i++)
            {

                s = request.sus[i].fieldType.ToLower();
                if (s == "int")
                {
                    Random rand = new Random();
                    answer = rand.Next(1, 13).ToString();
                    x.Add(request.sus[i].fieldName, answer);
                    row.Add(request.sus[i].fieldName, answer);
                }
            }
            data.Add(row);
            iterations--;
            x.Clear();
        }
        return Task.FromResult(data);

        //var one="";
        //var two="";
        //var three="";
        //var four="";

        //    one = request.sus[0].fieldType;
        //    two = request.sus[0].fieldValue;
        //    three = request.sus[1].fieldType;
        //    four = request.sus[1].fieldValue;
        


        //ResultModel[] P=new ResultModel[3];
        //P[0] = new ResultModel();
        //P[1] = new ResultModel();
        //P[2] = new ResultModel();
        //P[0].columnValue = "1";
        //P[1].columnValue = "2";
        //P[2].columnValue = "3";
        //P[0].fieldValue = "Akash";
        //P[1].fieldValue = "Krishna";
        //P[2].fieldValue = "Mohan";
        
        
        //return Task.FromResult(P);

    }
}