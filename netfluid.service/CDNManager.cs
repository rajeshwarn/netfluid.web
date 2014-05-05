namespace NetFluid.Service
{
    [Route("cdn")]
    class CDNManager:FluidPage
    {
        [Route("update")]
        public IResponse Update(string id, string host, string path)
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");
            CDNRepository.Update(id,host,path);
            return new FluidTemplate("./UI/admin.html");
        }

        [Route("add")]
        public IResponse Add(string host, string path)
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");
            CDNRepository.Add(host,path);
            return new FluidTemplate("./UI/admin.html");
        }

        [ParametrizedRoute("delete")]
        public IResponse Delete(string id)
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");
            CDNRepository.Delete(id);
            return new FluidTemplate("./UI/admin.html");
        }
    }
}
