namespace Baguettefy.Data.DofusDb.Requirements
{
    //Short data for either Achievement or Quest info
    public class RequirementInfo
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public class Requirements
    {
        public RequirementInfo? QuestData { get; set; }
        public RequirementInfo? AchievementData { get; set; }

        public List<long> RequirementIds
        {
            get
            {
                List<long> ids = new List<long>();
                PopulateChainIds(this, ids);
                return ids;
            }
        }

        private void PopulateChainIds(Requirements requirements, List<long> chain)
        {
            long getId(Requirements req)
            {
                if (req.AchievementData != null) return req.AchievementData.Id;
                return req.QuestData?.Id ?? 0;
            }

            var chainId = getId(requirements);
            if (chainId != 0)
            {
                chain.Add(chainId);
            }

            if (requirements.Required == null) return;
            foreach (var required in requirements.Required)
            {
                PopulateChainIds(required, chain);
            }
        }

        public List<Requirements>? Required { get; set; } = new List<Requirements>();


        public string ToMermaid()
        {
            string mermaid = "flowchart TD\r\n";
            int step = 0;

            UpdateMermaid(ref step, ref mermaid);

            return mermaid;
        }

        private void UpdateMermaid(ref int step, ref string mermaid)
        {
            var startStep = step;
            for (var index = 0; index < Required.Count; index++)
            {
                var quest = Required[index];
                //mermaid += $"\n    {startStep}({Data.Name}) --> {++step}({quest.Data.Name})";
            }

            foreach (Requirements quest in Required)
            {
                quest.UpdateMermaid(ref step, ref mermaid);
            }
        }

        public string ToPlantUml()
        {
            string plant = "";
            UpdatePlant(0, ref plant);

            var startGraph = @"@startmindmap";
            var start =
                        $"skinparam dpi {68}\n" +
                        $"scale max {4000} height\n" +
                        $"scale max {4000} width\n";

            var value = $"{startGraph}\n{start}{plant}\n@endmindmap";


            return value;
        }

        private static void PutAsteriks(ref string value, int count)
        {
            for (int i = 0; i <= count; i++)
            {
                value += "*";
            }
        }


        private void UpdatePlant(int step, ref string plant)
        {
            PutAsteriks(ref plant, step);
            plant += $" {(QuestData == null ? $"<:1f451:> {AchievementData.Name}" : $"<:1f4d6:> {QuestData.Name}")}\n";

            foreach (var req in Required)
            {
                PutAsteriks(ref plant, step + 1);
                plant +=
                    $" {(req.QuestData == null ? $"<:1f451:> {req.AchievementData.Name}" : $"<:1f4d6:> {req.QuestData.Name}")}\n";

                foreach (Requirements childReq in req.Required)
                {
                    childReq.UpdatePlant(step + 2, ref plant);
                }
            }
        }

    }
}
