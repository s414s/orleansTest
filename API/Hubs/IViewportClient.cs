﻿using API.DTOs;

namespace API.Hubs;

public interface IViewportClient
{
    Task ReceiveMessage(string message);
    Task SendStateChange(Pt atlasChange);
    Task InitializeState(List<Pt> points);
    Task Flush(List<AtlasChangeEvent> changes);
}
