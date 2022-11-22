﻿using CnGalWebSite.DataModel.Model;
using CnGalWebSite.DataModel.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Xml.Linq;

namespace CnGalWebSite.DataModel.ViewModel.Videos
{
    public class EditVideoMainPageViewModel : BaseEditModel
    {
        [Display(Name = "正文")]
        [Required(ErrorMessage = "请输入正文")]
        public string Context { get; set; }

        public override Result Validate()
        {
            if (string.IsNullOrWhiteSpace(Context))
            {
                return new Result { Error = "请输入正文" };
            }

            return new Result { Successful = true };
        }
    }
}
