#include <stdio.h>
#include <stdlib.h>
#include <uhd/usrp/multi_usrp.hpp>

using namespace uhd;
using namespace uhd::usrp;

multi_usrp::sptr s_Radio = NULL;
rx_streamer::sptr s_RX = NULL;

extern "C" __declspec(dllexport) int Init()
{
	try {
		device_addr_t params("");
		s_Radio = multi_usrp::make(params);
		stream_args_t stream_args("fc64", "sc16");
		s_RX = s_Radio->get_rx_stream(stream_args);
		return 0;
	} catch( uhd::key_error a ) {
		return -1;
	} catch( uhd::assertion_error a ) {
		return -1;
	}
}

extern "C" __declspec(dllexport) int Tune(double frequency, double rate)
{
	try {
		tune_request_t tuneRequest = tune_request_t(frequency);
		s_Radio->set_rx_freq(tuneRequest);
		return 0;
	} catch( uhd::key_error a ) {
		return -1;
	} catch( uhd::assertion_error a ) {
		return -1;
	}
}

extern "C" __declspec(dllexport) int SetSampleRate(double rate)
{
	try {
		s_Radio->set_rx_rate(rate);
		return 0;
	} catch( uhd::key_error a ) {
		return -1;
	} catch( uhd::assertion_error a ) {
		return -1;
	}
}

extern "C" __declspec(dllexport) int StartStreaming()
{
	try {
		stream_cmd_t stream_cmd(stream_cmd_t::STREAM_MODE_START_CONTINUOUS);
		stream_cmd.stream_now = true;
		stream_cmd.time_spec = uhd::time_spec_t();
		s_Radio->issue_stream_cmd(stream_cmd);
		return 0;
	} catch( uhd::key_error a ) {
		return -1;
	} catch( uhd::assertion_error a ) {
		return -1;
	}
}

struct df
{
	double r;
	double i;
};

extern "C" __declspec(dllexport) int GetSamples(double* samples, unsigned int maxSamples)
{
	uhd::rx_metadata_t md;
	int numSamples = s_RX->recv((df*)samples, maxSamples, md, 1.0);
	if (md.error_code == uhd::rx_metadata_t::ERROR_CODE_NONE)
	{
		return numSamples;
	}
	return 0;
}
