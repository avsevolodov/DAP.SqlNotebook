using AutoMapper;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using DAP.SqlNotebook.BL.Models;
using DAP.SqlNotebook.BL.Services.AiSql;
using DAP.SqlNotebook.Contract.Entities;
using DbEntityInfo = DAP.SqlNotebook.Contract.Entities.DbEntityInfo;
using DbFieldInfo = DAP.SqlNotebook.Contract.Entities.DbFieldInfo;

namespace DAP.SqlNotebook.Service.Mapper
{
    public class ManagementBLProfile : Profile
    {
        public ManagementBLProfile()
        {
            CreateMap<WorkspaceEntity, WorkspaceInfo>();
            CreateMap<CatalogNode, CatalogNodeInfo>();
            CreateMap<AiAssistMessageEntity, AiAssistMessageInfo>();
            CreateMap<AiAssistSessionEntity, AiAssistSessionInfo>();

            CreateMap<DAP.SqlNotebook.BL.Models.DbEntityInfo, DbEntityInfo>();
            CreateMap<DAP.SqlNotebook.BL.Models.DbFieldInfo, DbFieldInfo>();

            CreateMap<AiSqlRequestInfo, AiSqlGenerateRequest>();
            CreateMap<AiSqlGenerateResult, AiSqlResponseInfo>();
            CreateMap<FindTablesRequestInfo, FindTablesRequest>();
            CreateMap<FindTablesResult, FindTablesResponseInfo>();
            CreateMap<AiSqlAutocompleteRequestInfo, AiSqlAutocompleteRequest>();
            CreateMap<AiSqlSuggestionItem, AiSqlSuggestionItemInfo>();
            CreateMap<AiSqlAutocompleteResult, AiSqlAutocompleteResponseInfo>();
            CreateMap<SuggestChartRequestInfo, SuggestChartRequest>();
            CreateMap<SuggestChartResult, SuggestChartResponseInfo>();

            CreateMap<NotebookEntity, NotebookMetaInfo>().ConvertUsing<NotebookEntityToMetaInfoConverter>();
            CreateMap<NotebookEntity, NotebookInfo>().ConvertUsing<NotebookEntityToInfoConverter>();
            CreateMap<NotebookInfo, NotebookEntity>().ConvertUsing<NotebookInfoToEntityConverter>();
        }
    }
}
